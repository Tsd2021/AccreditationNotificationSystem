using ANS.Model;
using ANS.Model.DTOs;
using ANS.Model.GeneradorArchivoPorBanco;
using ANS.Runtime;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    /// <summary>
    /// Servicio para acreditación manual de depósitos por IDOPERACION
    /// </summary>
    public class ServicioAcreditacionManual
    {
        private string _conexionTSD = ConfiguracionGlobal.Conexion22;
        private string _conexionWebBuzones = ConfiguracionGlobal.ConexionWebBuzones;

        // ✅ Thread-safe: Lazy<T> garantiza inicialización única
        private static readonly Lazy<ServicioAcreditacionManual> _lazy =
            new Lazy<ServicioAcreditacionManual>(() => new ServicioAcreditacionManual());

        public static ServicioAcreditacionManual instancia => _lazy.Value;

        public static ServicioAcreditacionManual getInstancia()
        {
            return _lazy.Value;
        }

        /// <summary>
        /// Busca buzones por NN (nombre del buzón)
        /// </summary>
        public async Task<List<BuzonBusquedaDto>> BuscarBuzonesPorNN(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return new List<BuzonBusquedaDto>();

            var resultado = new List<BuzonBusquedaDto>();

            const string sql = @"
                SELECT DISTINCT 
                    c.NC, 
                    c.NN, 
                    c.SUCURSAL
                FROM cc c
                WHERE c.estado = 'alta'
                  AND (c.NN LIKE @texto OR c.NC LIKE @texto)
                ORDER BY c.NN";

            using var conn = new SqlConnection(_conexionTSD);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@texto", $"%{texto.Trim()}%");

            using var reader = await cmd.ExecuteReaderAsync();
            int ncOrd = reader.GetOrdinal("NC");
            int nnOrd = reader.GetOrdinal("NN");
            int sucursalOrd = reader.GetOrdinal("SUCURSAL");

            while (await reader.ReadAsync())
            {
                resultado.Add(new BuzonBusquedaDto
                {
                    NC = reader.GetString(ncOrd)?.Trim(),
                    NN = reader.GetString(nnOrd),
                    Sucursal = reader.GetString(sucursalOrd)
                });
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene empresas por buzón (desde ConfiguracionAcreditacion -> cuentasbuzones)
        /// </summary>
        public async Task<List<EmpresaDto>> ObtenerEmpresasPorBuzon(string nc)
        {
            if (string.IsNullOrWhiteSpace(nc))
                return new List<EmpresaDto>();

            var resultado = new List<EmpresaDto>();

            const string sql = @"
                SELECT DISTINCT
                    cb.EMPRESA,
                    cb.ID AS IdCuenta,
                    cb.CUENTA,
                    cb.MONEDA,
                    cb.BANCO,
                    b.BancoId AS IdBanco
                FROM ConfiguracionAcreditacion config
                INNER JOIN cuentasbuzones cb ON config.CuentasBuzonesId = cb.ID
                INNER JOIN cc c ON c.NC = config.NC AND c.IDCLIENTE = cb.IDCLIENTE
                LEFT JOIN (
                    SELECT BancoId, NombreBanco FROM (
                        VALUES 
                            (1, 'Santander'),
                            (2, 'BBVA'),
                            (3, 'Scotiabank'),
                            (4, 'Itau'),
                            (5, 'HSBC'),
                            (6, 'Bandes'),
                            (7, 'BROU'),
                            (8, 'Heritage')
                    ) AS Bancos(BancoId, NombreBanco)
                ) b ON UPPER(b.NombreBanco) = UPPER(cb.BANCO)
                WHERE config.NC = @nc
                  AND cb.EMPRESA IS NOT NULL
                  AND LTRIM(RTRIM(cb.EMPRESA)) <> ''
                ORDER BY cb.BANCO, cb.EMPRESA, cb.MONEDA";

            using var conn = new SqlConnection(_conexionTSD);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nc", nc.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            int empresaOrd = reader.GetOrdinal("EMPRESA");
            int idCuentaOrd = reader.GetOrdinal("IdCuenta");
            int cuentaOrd = reader.GetOrdinal("CUENTA");
            int monedaOrd = reader.GetOrdinal("MONEDA");
            int bancoOrd = reader.GetOrdinal("BANCO");
            int idBancoOrd = reader.GetOrdinal("IdBanco");

            var seenIds = new HashSet<int>();
            while (await reader.ReadAsync())
            {
                var idCuenta = reader.GetInt32(idCuentaOrd);
                if (seenIds.Contains(idCuenta))
                    continue;
                seenIds.Add(idCuenta);
                resultado.Add(new EmpresaDto
                {
                    Empresa = reader.GetString(empresaOrd),
                    IdCuenta = idCuenta,
                    Cuenta = reader.GetString(cuentaOrd),
                    Moneda = reader.IsDBNull(monedaOrd) ? null : reader.GetString(monedaOrd),
                    Banco = reader.IsDBNull(bancoOrd) ? null : reader.GetString(bancoOrd),
                    IdBanco = reader.IsDBNull(idBancoOrd) ? 0 : reader.GetInt32(idBancoOrd)
                });
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene depósitos Validado del buzón en el rango, enriquecidos con IdCuenta/Empresa/Banco/Moneda/Cuenta desde CUENTASBUZONES.
        /// Si idCuentaFiltro != null, solo devuelve depósitos de esa cuenta; si null, de todas las cuentas configuradas para el buzón.
        /// La vinculación depósito -> cuenta se hace por empresa normalizada + moneda (determinístico: una cuenta por Empresa+Moneda por buzón).
        /// </summary>
        public async Task<List<DepositoAcreditacionDto>> ObtenerDepositosPorBuzonEnRango(
            string nc,
            DateTime desde,
            DateTime hasta,
            int? idCuentaFiltro)
        {
            if (string.IsNullOrWhiteSpace(nc))
                return new List<DepositoAcreditacionDto>();

            var empresas = await ObtenerEmpresasPorBuzon(nc);
            if (!empresas.Any())
                return new List<DepositoAcreditacionDto>();

            // Query depósitos Validado del buzón en rango (sin filtro por empresa)
            const string query = @"
                SELECT 
                    d.iddeposito, 
                    d.idoperacion, 
                    d.codigo, 
                    d.tipo, 
                    CASE 
                        WHEN CHARINDEX('-', d.empresa) > 0 
                        THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                        ELSE LTRIM(RTRIM(d.empresa))
                    END AS empresa, 
                    d.fechadep,
                    d.usuario
                FROM Depositos d
                INNER JOIN relaciondeposito rd ON d.IdDeposito = rd.IdDeposito 
                INNER JOIN Totales t ON rd.IdTotal = t.IdTotal
                WHERE d.codigo = @nc
                  AND d.tipo = 'Validado'
                  AND d.fechadep >= @desde
                  AND d.fechadep < DATEADD(day, 1, @hasta)";

            var depositosConTotales = new Dictionary<int, (int IdDeposito, int IdOperacion, string Codigo, string Tipo, string Empresa, DateTime FechaDep, string Usuario, double TotalPesos, double TotalDolares)>();

            using (var conn = new SqlConnection(_conexionWebBuzones))
            {
                await conn.OpenAsync();

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@nc", nc.Trim());
                cmd.Parameters.AddWithValue("@desde", desde.Date);
                cmd.Parameters.AddWithValue("@hasta", hasta.Date);

                var depositosBase = new List<(int IdDeposito, int IdOperacion, string Codigo, string Tipo, string Empresa, DateTime FechaDep, string Usuario)>();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    int idDepOrd = reader.GetOrdinal("iddeposito");
                    int idOpOrd = reader.GetOrdinal("idoperacion");
                    int codigoOrd = reader.GetOrdinal("codigo");
                    int tipoOrd = reader.GetOrdinal("tipo");
                    int empresaOrd = reader.GetOrdinal("empresa");
                    int fechaOrd = reader.GetOrdinal("fechadep");
                    int usuarioOrd = reader.GetOrdinal("usuario");

                    while (await reader.ReadAsync())
                    {
                        depositosBase.Add((
                            reader.GetInt32(idDepOrd),
                            reader.GetInt32(idOpOrd),
                            reader.GetString(codigoOrd),
                            reader.GetString(tipoOrd),
                            reader.GetString(empresaOrd),
                            reader.GetDateTime(fechaOrd),
                            reader.IsDBNull(usuarioOrd) ? null : reader.GetString(usuarioOrd)
                        ));
                    }
                }

                if (!depositosBase.Any())
                    return new List<DepositoAcreditacionDto>();

                var idDepositos = depositosBase.Select(x => x.IdDeposito).Distinct().ToList();
                var totalesPorDeposito = await ObtenerTotalesPorDepositosBatch(idDepositos);

                foreach (var dep in depositosBase)
                {
                    if (!totalesPorDeposito.TryGetValue(dep.IdDeposito, out var totales))
                        totales = new List<(string Divisa, int ImporteTotal)>();

                    var totalPesos = totales.Where(t => t.Divisa == VariablesGlobales.uyu).Sum(t => t.ImporteTotal);
                    var totalDolares = totales.Where(t => t.Divisa == VariablesGlobales.usd).Sum(t => t.ImporteTotal);

                    if (!depositosConTotales.ContainsKey(dep.IdOperacion))
                    {
                        depositosConTotales[dep.IdOperacion] = (dep.IdDeposito, dep.IdOperacion, dep.Codigo, dep.Tipo, dep.Empresa, dep.FechaDep, dep.Usuario, totalPesos, totalDolares);
                    }
                    else
                    {
                        var ex = depositosConTotales[dep.IdOperacion];
                        depositosConTotales[dep.IdOperacion] = (ex.IdDeposito, ex.IdOperacion, ex.Codigo, ex.Tipo, ex.Empresa, ex.FechaDep, ex.Usuario, ex.TotalPesos + totalPesos, ex.TotalDolares + totalDolares);
                    }
                }
            }

            // Resolver IdCuenta por empresa normalizada + moneda (una cuenta por Empresa+Moneda)
            var cuentasPesos = empresas.Where(e => EsMonedaPesos(e.Moneda)).OrderBy(e => e.IdCuenta).ToList();
            var cuentasDolares = empresas.Where(e => EsMonedaDolares(e.Moneda)).OrderBy(e => e.IdCuenta).ToList();

            var resultado = new List<DepositoAcreditacionDto>();

            foreach (var dep in depositosConTotales.Values)
            {
                var depositoEmpresaNorm = NormalizarEmpresa(dep.Empresa);
                if (dep.TotalPesos > 0)
                {
                    var cuenta = ResolverCuentaPorEmpresaYMoneda(cuentasPesos, depositoEmpresaNorm);
                    if (cuenta != null)
                    {
                        var dto = new DepositoAcreditacionDto
                        {
                            IdDeposito = dep.IdDeposito,
                            IdOperacion = dep.IdOperacion,
                            Codigo = dep.Codigo,
                            Empresa = cuenta.Empresa,
                            FechaDep = dep.FechaDep,
                            Tipo = dep.Tipo,
                            Usuario = dep.Usuario,
                            IdCuenta = cuenta.IdCuenta,
                            Cuenta = cuenta.Cuenta,
                            Banco = cuenta.Banco,
                            IdBanco = cuenta.IdBanco,
                            Moneda = VariablesGlobales.pesos,
                            Divisa = VariablesGlobales.uyu,
                            MontoTotal = dep.TotalPesos,
                            HasCuentaAsignada = true
                        };
                        if (idCuentaFiltro == null || dto.IdCuenta == idCuentaFiltro.Value)
                            resultado.Add(dto);
                    }
                    else
                    {
                        LogDepositoSinCuentaAsignada(dep.IdDeposito, dep.IdOperacion, depositoEmpresaNorm, VariablesGlobales.pesos, cuentasPesos);
                        var dtoSinCuenta = CrearDtoSinCuentaAsignada(dep, VariablesGlobales.pesos, VariablesGlobales.uyu, dep.TotalPesos);
                        if (idCuentaFiltro == null)
                            resultado.Add(dtoSinCuenta);
                    }
                }
                if (dep.TotalDolares > 0)
                {
                    var cuenta = ResolverCuentaPorEmpresaYMoneda(cuentasDolares, depositoEmpresaNorm);
                    if (cuenta != null)
                    {
                        var dto = new DepositoAcreditacionDto
                        {
                            IdDeposito = dep.IdDeposito,
                            IdOperacion = dep.IdOperacion,
                            Codigo = dep.Codigo,
                            Empresa = cuenta.Empresa,
                            FechaDep = dep.FechaDep,
                            Tipo = dep.Tipo,
                            Usuario = dep.Usuario,
                            IdCuenta = cuenta.IdCuenta,
                            Cuenta = cuenta.Cuenta,
                            Banco = cuenta.Banco,
                            IdBanco = cuenta.IdBanco,
                            Moneda = VariablesGlobales.dolares,
                            Divisa = VariablesGlobales.usd,
                            MontoTotal = dep.TotalDolares,
                            HasCuentaAsignada = true
                        };
                        if (idCuentaFiltro == null || dto.IdCuenta == idCuentaFiltro.Value)
                            resultado.Add(dto);
                    }
                    else
                    {
                        LogDepositoSinCuentaAsignada(dep.IdDeposito, dep.IdOperacion, depositoEmpresaNorm, VariablesGlobales.dolares, cuentasDolares);
                        var dtoSinCuenta = CrearDtoSinCuentaAsignada(dep, VariablesGlobales.dolares, VariablesGlobales.usd, dep.TotalDolares);
                        if (idCuentaFiltro == null)
                            resultado.Add(dtoSinCuenta);
                    }
                }
            }

            return await MapearDepositosConEstadoAcreditado(resultado);
        }

        private static bool EsMonedaPesos(string moneda)
        {
            if (string.IsNullOrWhiteSpace(moneda)) return false;
            var m = moneda.Trim().ToUpperInvariant();
            return m == "PESOS" || m == "UYU";
        }

        private static bool EsMonedaDolares(string moneda)
        {
            if (string.IsNullOrWhiteSpace(moneda)) return false;
            var m = moneda.Trim().ToUpperInvariant();
            return m == "DOLARES" || m == "USD";
        }

        /// <summary>
        /// Normaliza el texto "empresa" del depósito igual que el CASE en ServicioDeposito:
        /// recorta el sufijo después del último '-' y aplica trim; si no hay '-', solo trim.
        /// </summary>
        private static string NormalizarEmpresa(string empresaRaw)
        {
            if (string.IsNullOrEmpty(empresaRaw))
                return string.Empty;
            var lastDash = empresaRaw.LastIndexOf('-');
            if (lastDash >= 0)
                return empresaRaw.Substring(lastDash + 1).Trim();
            return empresaRaw.Trim();
        }

        /// <summary>
        /// Indica si para (banco, empresaCuenta) debe usarse match exacto (como en ServicioDeposito).
        /// Lista de excepciones: Santander (BAS, SANTANDER ECHEDO, PONILOR, etc.) y BBVA (ECHEDO, FERRECAR, etc.).
        /// Comparación case-insensitive.
        /// </summary>
        private static bool RequiereMatchExacto(string banco, string empresaCuenta)
        {
            var bancoU = (banco ?? "").Trim().ToUpperInvariant();
            var empU = (empresaCuenta ?? "").Trim().ToUpperInvariant();
            if (bancoU == VariablesGlobales.santander.ToUpperInvariant())
            {
                return empU == "BAS" || empU == "SANTANDER ECHEDO" || empU == "PONILOR"
                    || empU == "PONILOR PUNTA DE SANTIAGO" || empU == "PONILOR SAS LIBERTADOR" || empU == "PONILOR SAS";
            }
            if (bancoU == VariablesGlobales.bbva.ToUpperInvariant())
            {
                return empU == "ECHEDO" || empU == "FERRECAR MARCELOFERRERO" || empU == "FERRECAR" || empU == "EUROPEAN";
            }
            return false;
        }

        /// <summary>
        /// Evalúa si el depósito hace match con la cuenta según la policy de ServicioDeposito:
        /// - Si RequiereMatchExacto(banco, empresaCuenta): igualdad exacta (case-insensitive).
        /// - Si no: depositoEmpresaNorm CONTIENE empresaCuentaNorm (como LIKE '%empresaCuenta%').
        /// No se usa "empresaCuenta contiene deposito". Si empresaCuentaNorm está vacía, devuelve false.
        /// </summary>
        private static bool MatchEmpresa(string depositoEmpresaNorm, string bancoCuenta, string empresaCuentaNorm)
        {
            if (string.IsNullOrWhiteSpace(empresaCuentaNorm))
                return false;
            var depNorm = (depositoEmpresaNorm ?? "").Trim();
            var cuentaNorm = empresaCuentaNorm.Trim();
            if (RequiereMatchExacto(bancoCuenta, empresaCuentaNorm))
                return string.Equals(depNorm, cuentaNorm, StringComparison.OrdinalIgnoreCase);
            return depNorm.IndexOf(cuentaNorm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Registra warning cuando un depósito no matchea con ninguna cuenta (misma policy que ServicioDeposito).
        /// </summary>
        private void LogDepositoSinCuentaAsignada(int idDeposito, int idOperacion, string depositoEmpresaNorm, string moneda, List<EmpresaDto> cuentasCandidatas)
        {
            var candidatasStr = cuentasCandidatas == null || !cuentasCandidatas.Any()
                ? "ninguna"
                : string.Join("; ", cuentasCandidatas.Select(c => $"{c.IdCuenta}:{c.Empresa ?? ""}"));
            ServicioLog.instancia.WriteWarning(
                $"Depósito sin cuenta asignada | IdDeposito: {idDeposito} | IdOperacion: {idOperacion} | EmpresaNorm: {depositoEmpresaNorm ?? ""} | Moneda: {moneda} | Candidatas: {candidatasStr}",
                "ServicioAcreditacionManual | ObtenerDepositosPorBuzonEnRango");
        }

        /// <summary>
        /// Crea un DTO de depósito sin cuenta asignada (se muestra en grilla pero no es acreditable).
        /// </summary>
        private static DepositoAcreditacionDto CrearDtoSinCuentaAsignada(
            (int IdDeposito, int IdOperacion, string Codigo, string Tipo, string Empresa, DateTime FechaDep, string Usuario, double TotalPesos, double TotalDolares) dep,
            string moneda, string divisa, double montoTotal)
        {
            return new DepositoAcreditacionDto
            {
                IdDeposito = dep.IdDeposito,
                IdOperacion = dep.IdOperacion,
                Codigo = dep.Codigo,
                Empresa = dep.Empresa,
                FechaDep = dep.FechaDep,
                Tipo = dep.Tipo,
                Usuario = dep.Usuario,
                IdCuenta = 0,
                Cuenta = null,
                Banco = null,
                IdBanco = 0,
                Moneda = moneda,
                Divisa = divisa,
                MontoTotal = montoTotal,
                HasCuentaAsignada = false,
                IsSelected = false
            };
        }

        /// <summary>
        /// Resuelve la cuenta (IdCuenta) para un depósito replicando la policy de ServicioDeposito.
        /// depositoEmpresaNorm debe venir ya normalizada (NormalizarEmpresa). Si hay varios matches,
        /// prefiere exactos y luego menor IdCuenta. Devuelve null si no hay match.
        /// </summary>
        private static EmpresaDto ResolverCuentaPorEmpresaYMoneda(List<EmpresaDto> cuentasMoneda, string depositoEmpresaNorm)
        {
            if (string.IsNullOrWhiteSpace(depositoEmpresaNorm) || cuentasMoneda == null || !cuentasMoneda.Any())
                return null;
            var depNorm = depositoEmpresaNorm.Trim();
            var candidatas = cuentasMoneda.Where(c =>
            {
                var cuentaEmpNorm = NormalizarEmpresa(c.Empresa);
                return MatchEmpresa(depNorm, c.Banco ?? "", cuentaEmpNorm);
            }).ToList();
            if (!candidatas.Any())
                return null;
            // Preferir matches exactos (depositoEmpresaNorm == cuenta.Empresa normalizada)
            var exactas = candidatas.Where(c =>
                string.Equals(depNorm, NormalizarEmpresa(c.Empresa), StringComparison.OrdinalIgnoreCase)).ToList();
            var elegidas = exactas.Any() ? exactas : candidatas;
            return elegidas.OrderBy(c => c.IdCuenta).First();
        }

        /// <summary>
        /// Obtiene totales por varios depósitos en una sola consulta (evita N+1).
        /// </summary>
        private async Task<Dictionary<int, List<(string Divisa, int ImporteTotal)>>> ObtenerTotalesPorDepositosBatch(List<int> idDepositos)
        {
            var resultado = new Dictionary<int, List<(string Divisa, int ImporteTotal)>>();
            if (idDepositos == null || !idDepositos.Any())
                return resultado;

            using var conn = new SqlConnection(_conexionWebBuzones);
            await conn.OpenAsync();

            // Construir IN (@p0, @p1, ...) con hasta 2100 parámetros
            var paramNames = new List<string>();
            var parametros = new List<SqlParameter>();
            for (int i = 0; i < idDepositos.Count && i < 2100; i++)
            {
                var name = $"@idDep{i}";
                paramNames.Add(name);
                parametros.Add(new SqlParameter(name, idDepositos[i]));
            }

            var inClause = string.Join(", ", paramNames);
            var sql = $@"
                SELECT relaciondeposito.iddeposito, totales.divisas, totales.importetotal
                FROM totales
                INNER JOIN relaciondeposito ON totales.IdTotal = relaciondeposito.IdTotal
                WHERE relaciondeposito.iddeposito IN ({inClause})";

            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parametros)
                cmd.Parameters.Add(p);

            using var reader = await cmd.ExecuteReaderAsync();
            int idDepOrd = reader.GetOrdinal("iddeposito");
            int divisaOrd = reader.GetOrdinal("divisas");
            int importeOrd = reader.GetOrdinal("importetotal");

            while (await reader.ReadAsync())
            {
                var idDep = reader.GetInt32(idDepOrd);
                var divisa = reader.GetString(divisaOrd);
                var importe = reader.GetInt32(importeOrd);
                if (!resultado.TryGetValue(idDep, out var list))
                {
                    list = new List<(string Divisa, int ImporteTotal)>();
                    resultado[idDep] = list;
                }
                list.Add((divisa, importe));
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene operaciones (depósitos Validado) del buzón para un día y marca IsAcreditado según tabla de acreditaciones.
        /// Reutilizado por Envío Manual para mostrar grilla con estado verde/rojo. Consulta WebBuzones + batch en TSD.
        /// </summary>
        public async Task<List<OperacionEnvioDto>> ObtenerOperacionesParaBuzonYFecha(string nc, DateTime fecha)
        {
            if (string.IsNullOrWhiteSpace(nc))
                return new List<OperacionEnvioDto>();

            var desde = fecha.Date;
            var hasta = fecha.Date.AddDays(1);

            const string query = @"
                SELECT 
                    d.iddeposito, 
                    d.idoperacion, 
                    d.codigo, 
                    d.tipo, 
                    CASE 
                        WHEN CHARINDEX('-', d.empresa) > 0 
                        THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                        ELSE LTRIM(RTRIM(d.empresa))
                    END AS empresa, 
                    d.fechadep
                FROM Depositos d
                INNER JOIN relaciondeposito rd ON d.IdDeposito = rd.IdDeposito 
                INNER JOIN Totales t ON rd.IdTotal = t.IdTotal
                WHERE d.codigo = @nc
                  AND d.tipo = 'Validado'
                  AND d.fechadep >= @desde
                  AND d.fechadep < @hasta";

            var depositosBase = new List<(int IdDeposito, int IdOperacion, string Codigo, string Tipo, string Empresa, DateTime FechaDep)>();
            var operacionesPorId = new Dictionary<int, (string Empresa, DateTime FechaDep, double Monto)>();

            using (var conn = new SqlConnection(_conexionWebBuzones))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nc", nc.Trim());
                    cmd.Parameters.AddWithValue("@desde", desde);
                    cmd.Parameters.AddWithValue("@hasta", hasta);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int idDepOrd = reader.GetOrdinal("iddeposito");
                        int idOpOrd = reader.GetOrdinal("idoperacion");
                        int codigoOrd = reader.GetOrdinal("codigo");
                        int tipoOrd = reader.GetOrdinal("tipo");
                        int empresaOrd = reader.GetOrdinal("empresa");
                        int fechaOrd = reader.GetOrdinal("fechadep");
                        while (await reader.ReadAsync())
                        {
                            depositosBase.Add((
                                reader.GetInt32(idDepOrd),
                                reader.GetInt32(idOpOrd),
                                reader.GetString(codigoOrd),
                                reader.GetString(tipoOrd),
                                reader.GetString(empresaOrd),
                                reader.GetDateTime(fechaOrd)
                            ));
                        }
                    }
                }

                if (!depositosBase.Any())
                    return new List<OperacionEnvioDto>();

                var idDepositos = depositosBase.Select(x => x.IdDeposito).Distinct().ToList();
                var totalesPorDeposito = await ObtenerTotalesPorDepositosBatch(idDepositos);

                foreach (var dep in depositosBase)
                {
                    if (!totalesPorDeposito.TryGetValue(dep.IdDeposito, out var totales))
                        totales = new List<(string Divisa, int ImporteTotal)>();
                    var totalPesos = totales.Where(t => t.Divisa == VariablesGlobales.uyu).Sum(t => t.ImporteTotal);
                    var totalDolares = totales.Where(t => t.Divisa == VariablesGlobales.usd).Sum(t => t.ImporteTotal);
                    var monto = totalPesos + totalDolares;
                    if (!operacionesPorId.ContainsKey(dep.IdOperacion))
                        operacionesPorId[dep.IdOperacion] = (dep.Empresa, dep.FechaDep, monto);
                    else
                    {
                        var ex = operacionesPorId[dep.IdOperacion];
                        operacionesPorId[dep.IdOperacion] = (ex.Empresa, ex.FechaDep, ex.Monto + monto);
                    }
                }
            }

            var idOperaciones = operacionesPorId.Keys.ToList();
            var acreditados = await ObtenerIdOperacionesAcreditados(nc.Trim(), idOperaciones);

            var resultado = new List<OperacionEnvioDto>();
            foreach (var kv in operacionesPorId)
            {
                resultado.Add(new OperacionEnvioDto
                {
                    IdOperacion = kv.Key,
                    Empresa = kv.Value.Empresa,
                    FechaDep = kv.Value.FechaDep,
                    Monto = kv.Value.Monto,
                    IsAcreditado = acreditados.Contains(kv.Key)
                });
            }
            return resultado.OrderBy(x => x.IdOperacion).ToList();
        }

        /// <summary>
        /// Batch: devuelve HashSet de IdOperacion que existen en tabla de acreditaciones para el buzón.
        /// </summary>
        private async Task<HashSet<int>> ObtenerIdOperacionesAcreditados(string nc, List<int> idOperaciones)
        {
            var resultado = new HashSet<int>();
            if (idOperaciones == null || !idOperaciones.Any())
                return resultado;

            var tableName = TableNameResolver.AcreditacionDeposito;
            TableNameResolver.ValidateTableName(tableName, "ServicioAcreditacionManual.ObtenerIdOperacionesAcreditados");

            var paramNames = new List<string>();
            var parametros = new List<SqlParameter> { new SqlParameter("@nc", nc) };
            for (int i = 0; i < idOperaciones.Count && i < 2100; i++)
            {
                var name = $"@idOp{i}";
                paramNames.Add(name);
                parametros.Add(new SqlParameter(name, idOperaciones[i]));
            }
            var inClause = string.Join(", ", paramNames);
            var sql = $@"SELECT DISTINCT IDOPERACION FROM {tableName} WHERE IDBUZON = @nc AND IDOPERACION IN ({inClause})";

            using var conn = new SqlConnection(_conexionTSD);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parametros)
                cmd.Parameters.Add(p);
            using var reader = await cmd.ExecuteReaderAsync();
            int opOrd = reader.GetOrdinal("IDOPERACION");
            while (await reader.ReadAsync())
                resultado.Add(reader.GetInt32(opOrd));
            return resultado;
        }

        /// <summary>
        /// Obtiene depósitos de los últimos 7 días (o rango personalizado) para un buzón/empresa/moneda.
        /// Si se pasa idCuenta, se usa solo esa CuentaBuzon (la seleccionada en el combo Banco+Empresa+Moneda).
        /// El Banco de cada DTO viene de cuentasbuzones.BANCO de esa cuenta, no de CC.
        /// </summary>
        public async Task<List<DepositoAcreditacionDto>> ObtenerDepositosUltimos7Dias(
            string nc, 
            string empresa, 
            DateTime desde, 
            DateTime hasta, 
            string moneda = null,
            int? idCuenta = null)
        {
            if (string.IsNullOrWhiteSpace(nc) || string.IsNullOrWhiteSpace(empresa))
                return new List<DepositoAcreditacionDto>();

            var depositos = new List<DepositoAcreditacionDto>();

            var empresas = await ObtenerEmpresasPorBuzon(nc);
            List<EmpresaDto> cuentasFiltradas;

            if (idCuenta.HasValue && idCuenta.Value != 0)
            {
                // Usar exactamente la cuenta seleccionada (Banco+Empresa+Moneda del combo)
                cuentasFiltradas = empresas.Where(e => e.IdCuenta == idCuenta.Value).ToList();
            }
            else
            {
                // Fallback: filtrar por empresa y moneda (puede haber varias cuentas por banco)
                cuentasFiltradas = empresas
                    .Where(e => e.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(moneda) || e.Moneda.Equals(moneda?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!cuentasFiltradas.Any())
                return depositos;

            // Construir query para obtener depósitos desde WEBBUZONES
            // Similar a ServicioDeposito pero con rango de fechas
            var query = @"
                SELECT 
                    d.iddeposito, 
                    d.idoperacion, 
                    d.codigo, 
                    d.tipo, 
                    CASE 
                        WHEN CHARINDEX('-', d.empresa) > 0 
                        THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                        ELSE LTRIM(RTRIM(d.empresa))
                    END AS empresa, 
                    d.fechadep,
                    d.usuario
                FROM Depositos d
                INNER JOIN relaciondeposito rd ON d.IdDeposito = rd.IdDeposito 
                INNER JOIN Totales t ON rd.IdTotal = t.IdTotal
                WHERE d.codigo = @nc
                  AND d.tipo = 'Validado'
                  AND d.fechadep >= @desde
                  AND d.fechadep < DATEADD(day, 1, @hasta)
                  AND (
                    CASE 
                        WHEN CHARINDEX('-', d.empresa) > 0 
                        THEN LTRIM(RTRIM(SUBSTRING(d.empresa, LEN(d.empresa) - CHARINDEX('-', REVERSE(d.empresa)) + 2, LEN(d.empresa))))
                        ELSE LTRIM(RTRIM(d.empresa))
                    END
                  ) LIKE '%' + @empresa + '%'";

            using var conn = new SqlConnection(_conexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@nc", nc.Trim());
            cmd.Parameters.AddWithValue("@empresa", empresa.Trim());
            cmd.Parameters.AddWithValue("@desde", desde.Date);
            cmd.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1));

            var depositosBase = new List<(int IdDeposito, int IdOperacion, string Codigo, string Tipo, string Empresa, DateTime FechaDep, string Usuario)>();

            using var reader = await cmd.ExecuteReaderAsync();
            int idDepOrd = reader.GetOrdinal("iddeposito");
            int idOpOrd = reader.GetOrdinal("idoperacion");
            int codigoOrd = reader.GetOrdinal("codigo");
            int tipoOrd = reader.GetOrdinal("tipo");
            int empresaOrd = reader.GetOrdinal("empresa");
            int fechaOrd = reader.GetOrdinal("fechadep");
            int usuarioOrd = reader.GetOrdinal("usuario");

            while (await reader.ReadAsync())
            {
                var idDep = reader.GetInt32(idDepOrd);
                var idOp = reader.GetInt32(idOpOrd);
                var codigo = reader.GetString(codigoOrd);
                var tipo = reader.GetString(tipoOrd);
                var emp = reader.GetString(empresaOrd);
                var fecha = reader.GetDateTime(fechaOrd);
                var usuario = reader.IsDBNull(usuarioOrd) ? null : reader.GetString(usuarioOrd);

                depositosBase.Add((idDep, idOp, codigo, tipo, emp, fecha, usuario));
            }

            // Obtener totales para cada depósito y agrupar por IdOperacion
            var depositosConTotales = new Dictionary<int, (int IdDeposito, int IdOperacion, string Codigo, string Tipo, string Empresa, DateTime FechaDep, string Usuario, double TotalPesos, double TotalDolares)>();

            foreach (var depBase in depositosBase)
            {
                var totales = await ObtenerTotalesPorDeposito(depBase.IdDeposito);
                var totalPesos = totales.Where(t => t.Divisa == VariablesGlobales.uyu).Sum(t => t.ImporteTotal);
                var totalDolares = totales.Where(t => t.Divisa == VariablesGlobales.usd).Sum(t => t.ImporteTotal);

                // Agrupar por IdOperacion (tomar el primero si hay duplicados)
                if (!depositosConTotales.ContainsKey(depBase.IdOperacion))
                {
                    depositosConTotales[depBase.IdOperacion] = (
                        depBase.IdDeposito,
                        depBase.IdOperacion,
                        depBase.Codigo,
                        depBase.Tipo,
                        depBase.Empresa,
                        depBase.FechaDep,
                        depBase.Usuario,
                        totalPesos,
                        totalDolares
                    );
                }
                else
                {
                    // Si ya existe, sumar los totales
                    var existente = depositosConTotales[depBase.IdOperacion];
                    depositosConTotales[depBase.IdOperacion] = (
                        existente.IdDeposito,
                        existente.IdOperacion,
                        existente.Codigo,
                        existente.Tipo,
                        existente.Empresa,
                        existente.FechaDep,
                        existente.Usuario,
                        existente.TotalPesos + totalPesos,
                        existente.TotalDolares + totalDolares
                    );
                }
            }

            // Mapear a DTOs: solo la moneda seleccionada. Banco/Cuenta vienen de la CuentaBuzon elegida (cuentasbuzones), no de CC.
            var incluirPesos = string.IsNullOrWhiteSpace(moneda) || moneda.Trim().Equals(VariablesGlobales.pesos, StringComparison.OrdinalIgnoreCase);
            var incluirDolares = string.IsNullOrWhiteSpace(moneda) || moneda.Trim().Equals(VariablesGlobales.dolares, StringComparison.OrdinalIgnoreCase);

            foreach (var dep in depositosConTotales.Values)
            {
                if (incluirPesos && dep.TotalPesos > 0)
                {
                    var cuentaPesos = cuentasFiltradas.FirstOrDefault(c =>
                        c.Moneda.Equals(VariablesGlobales.pesos, StringComparison.OrdinalIgnoreCase));

                    if (cuentaPesos != null)
                    {
                        depositos.Add(new DepositoAcreditacionDto
                        {
                            IdDeposito = dep.IdDeposito,
                            IdOperacion = dep.IdOperacion,
                            Codigo = dep.Codigo,
                            Empresa = dep.Empresa,
                            FechaDep = dep.FechaDep,
                            Tipo = dep.Tipo,
                            Usuario = dep.Usuario,
                            IdCuenta = cuentaPesos.IdCuenta,
                            Cuenta = cuentaPesos.Cuenta,
                            Banco = cuentaPesos.Banco,   // cuentasbuzones.BANCO (cuenta elegida), no CC
                            IdBanco = cuentaPesos.IdBanco,
                            Moneda = VariablesGlobales.pesos,
                            Divisa = VariablesGlobales.uyu,
                            MontoTotal = dep.TotalPesos
                        });
                    }
                }

                if (incluirDolares && dep.TotalDolares > 0)
                {
                    var cuentaDolares = cuentasFiltradas.FirstOrDefault(c =>
                        c.Moneda.Equals(VariablesGlobales.dolares, StringComparison.OrdinalIgnoreCase));

                    if (cuentaDolares != null)
                    {
                        depositos.Add(new DepositoAcreditacionDto
                        {
                            IdDeposito = dep.IdDeposito,
                            IdOperacion = dep.IdOperacion,
                            Codigo = dep.Codigo,
                            Empresa = dep.Empresa,
                            FechaDep = dep.FechaDep,
                            Tipo = dep.Tipo,
                            Usuario = dep.Usuario,
                            IdCuenta = cuentaDolares.IdCuenta,
                            Cuenta = cuentaDolares.Cuenta,
                            Banco = cuentaDolares.Banco, // cuentasbuzones.BANCO (cuenta elegida), no CC
                            IdBanco = cuentaDolares.IdBanco,
                            Moneda = VariablesGlobales.dolares,
                            Divisa = VariablesGlobales.usd,
                            MontoTotal = dep.TotalDolares
                        });
                    }
                }
            }

            return depositos;
        }

        /// <summary>
        /// Obtiene totales por depósito y divisa
        /// </summary>
        private async Task<List<(string Divisa, int ImporteTotal)>> ObtenerTotalesPorDeposito(int idDeposito)
        {
            var totales = new List<(string Divisa, int ImporteTotal)>();

            const string sqlTotales = @"
                SELECT totales.divisas, totales.importetotal
                FROM totales
                INNER JOIN relaciondeposito ON totales.IdTotal = relaciondeposito.IdTotal
                WHERE relaciondeposito.iddeposito = @idDep";

            using var conn = new SqlConnection(_conexionWebBuzones);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sqlTotales, conn);
            cmd.Parameters.AddWithValue("@idDep", idDeposito);

            using var reader = await cmd.ExecuteReaderAsync();
            int divisaOrd = reader.GetOrdinal("divisas");
            int importeOrd = reader.GetOrdinal("importetotal");

            while (await reader.ReadAsync())
            {
                var divisa = reader.GetString(divisaOrd);
                var importe = reader.GetInt32(importeOrd);
                totales.Add((divisa, importe));
            }

            return totales;
        }

        /// <summary>
        /// Verifica qué depósitos ya existen en BD usando clave funcional completa:
        /// (IDBUZON, IDOPERACION, MONEDA, IDCUENTA, MONTO, FECHADEP)
        /// Retorna HashSet con las claves existentes para filtrado eficiente
        /// </summary>
        private async Task<HashSet<(string NC, long IdOperacion, int MonedaId, int IdCuenta, double Monto, DateTime FechaDep)>> VerificarDepositosExistentes(
            List<DepositoAcreditacionDto> depositos)
        {
            var existentes = new HashSet<(string NC, long IdOperacion, int MonedaId, int IdCuenta, double Monto, DateTime FechaDep)>();

            if (depositos == null || !depositos.Any())
                return existentes;

            // Construir claves funcionales completas
            var claves = depositos.Select(d => new
            {
                NC = d.Codigo?.Trim(),
                IdOperacion = (long)d.IdOperacion,
                MonedaId = d.Moneda == VariablesGlobales.pesos ? 1 : 2,
                IdCuenta = d.IdCuenta,
                Monto = d.MontoTotal,
                FechaDep = d.FechaDep.Date // Normalizar a fecha sin hora
            }).Distinct().ToList();

            if (!claves.Any())
                return existentes;

            // Consulta batch usando condiciones OR (más simple que TVP, funciona bien para hasta ~1000 registros)
            var condiciones = new List<string>();
            var parametros = new List<SqlParameter>();

            for (int i = 0; i < claves.Count; i++)
            {
                var clave = claves[i];
                condiciones.Add($@"(IDBUZON = @nc{i} 
                    AND IDOPERACION = @idOp{i} 
                    AND MONEDA = @moneda{i} 
                    AND IDCUENTA = @idCuenta{i} 
                    AND CAST(FECHADEP AS DATE) = @fechaDep{i}
                    AND ABS(MONTO - @monto{i}) < 0.0001)");
                parametros.Add(new SqlParameter($"@nc{i}", clave.NC ?? (object)DBNull.Value));
                parametros.Add(new SqlParameter($"@idOp{i}", clave.IdOperacion));
                parametros.Add(new SqlParameter($"@moneda{i}", clave.MonedaId));
                parametros.Add(new SqlParameter($"@idCuenta{i}", clave.IdCuenta));
                parametros.Add(new SqlParameter($"@monto{i}", clave.Monto));
                parametros.Add(new SqlParameter($"@fechaDep{i}", clave.FechaDep));
            }

            if (!condiciones.Any())
                return existentes;

            // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
            var tableName = TableNameResolver.AcreditacionDeposito;
            TableNameResolver.ValidateTableName(tableName, "ServicioAcreditacionManual.VerificarDepositosExistentes");

            var sql = $@"
                SELECT 
                    IDBUZON,
                    IDOPERACION,
                    MONEDA,
                    IDCUENTA,
                    MONTO,
                    FECHADEP
                FROM {tableName}
                WHERE ({string.Join(" OR ", condiciones)})";

            using var conn = new SqlConnection(_conexionTSD);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            foreach (var param in parametros)
            {
                cmd.Parameters.Add(param);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            int ncOrd = reader.GetOrdinal("IDBUZON");
            int opOrd = reader.GetOrdinal("IDOPERACION");
            int monedaOrd = reader.GetOrdinal("MONEDA");
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int montoOrd = reader.GetOrdinal("MONTO");
            int fechaDepOrd = reader.GetOrdinal("FECHADEP");

            while (await reader.ReadAsync())
            {
                var nc = reader.GetString(ncOrd)?.Trim();
                var idOp = reader.GetInt64(opOrd);
                var moneda = reader.GetInt32(monedaOrd);
                var idCuenta = reader.GetInt32(cuentaOrd);
                var monto = reader.GetDouble(montoOrd);
                var fechaDep = reader.GetDateTime(fechaDepOrd).Date;

                existentes.Add((nc, idOp, moneda, idCuenta, monto, fechaDep));
            }

            return existentes;
        }

        /// <summary>
        /// Mapea depósitos con su estado de acreditación. IsAcreditado = true si existe en tabla de acreditaciones
        /// un registro con IDBUZON = deposito.Codigo (NC) e IDOPERACION = deposito.IdOperacion (no depende de IDCUENTA ni MONEDA).
        /// </summary>
        public async Task<List<DepositoAcreditacionDto>> MapearDepositosConEstadoAcreditado(List<DepositoAcreditacionDto> depositos)
        {
            if (depositos == null || !depositos.Any())
                return depositos;

            // Claves únicas (NC, IdOperacion) para batch; existencia por IDBUZON+IDOPERACION (no IDCUENTA ni MONEDA)
            var clavesUnicas = depositos
                .Select(d => (NC: d.Codigo?.Trim(), IdOp: (long)d.IdOperacion))
                .Distinct()
                .ToList();

            if (!clavesUnicas.Any())
                return depositos;

            var condiciones = new List<string>();
            var parametros = new List<SqlParameter>();

            for (int i = 0; i < clavesUnicas.Count; i++)
            {
                var clave = clavesUnicas[i];
                condiciones.Add($"(IDBUZON = @nc{i} AND IDOPERACION = @idOp{i})");
                parametros.Add(new SqlParameter($"@nc{i}", (object)clave.NC ?? DBNull.Value));
                parametros.Add(new SqlParameter($"@idOp{i}", clave.IdOp));
            }

            if (!condiciones.Any())
                return depositos;

            var tableName = TableNameResolver.AcreditacionDeposito;
            TableNameResolver.ValidateTableName(tableName, "ServicioAcreditacionManual.MapearDepositosConEstadoAcreditado");

            var sql = $@"
                SELECT IDBUZON, IDOPERACION, MONTO, FECHA
                FROM {tableName}
                WHERE ({string.Join(" OR ", condiciones)})";

            // Si hay varias filas para la misma (NC, IdOp) —p. ej. por cuenta/moneda— nos quedamos con la de FECHA más reciente (determinístico)
            var acreditacionesExistentes = new Dictionary<(string NC, long IdOp), (double Monto, DateTime Fecha)>();

            using (var conn = new SqlConnection(_conexionTSD))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand(sql, conn))
                {
                    foreach (var param in parametros)
                        cmd.Parameters.Add(param);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int ncOrd = reader.GetOrdinal("IDBUZON");
                        int opOrd = reader.GetOrdinal("IDOPERACION");
                        int montoOrd = reader.GetOrdinal("MONTO");
                        int fechaOrd = reader.GetOrdinal("FECHA");

                        while (await reader.ReadAsync())
                        {
                            var nc = reader.GetString(ncOrd)?.Trim();
                            var idOp = reader.GetInt64(opOrd);
                            var monto = reader.GetDouble(montoOrd);
                            var fecha = reader.GetDateTime(fechaOrd);

                            var key = (nc, idOp);
                            if (acreditacionesExistentes.TryGetValue(key, out var existente))
                            {
                                if (fecha > existente.Fecha)
                                    acreditacionesExistentes[key] = (monto, fecha);
                            }
                            else
                            {
                                acreditacionesExistentes[key] = (monto, fecha);
                            }
                        }
                    }
                }
            }

            foreach (var deposito in depositos)
            {
                var clave = (deposito.Codigo?.Trim(), (long)deposito.IdOperacion);

                if (acreditacionesExistentes.TryGetValue(clave, out var acreditacion))
                {
                    deposito.IsAcreditado = true;
                    deposito.FechaAcreditacion = acreditacion.Fecha;
                    deposito.MontoAcreditado = acreditacion.Monto;

                    // Monto es referencial (puede haber varias filas por cuenta/moneda); comparamos contra el de la fila con FECHA más reciente
                    if (Math.Abs(deposito.MontoTotal - acreditacion.Monto) > 0.01)
                    {
                        ServicioLog.instancia.WriteWarning(
                            $"Depósito ya acreditado con monto diferente | NC: {deposito.Codigo} | IDOperacion: {deposito.IdOperacion} | Monto actual: {deposito.MontoTotal} | Monto acreditado (ref): {acreditacion.Monto}",
                            "ServicioAcreditacionManual | MapearDepositosConEstadoAcreditado");
                    }
                }
            }

            return depositos;
        }

        /// <summary>
        /// Acredita múltiples depósitos (genera archivos por banco + inserta en BD de forma transaccional)
        /// Idempotente: filtra depósitos que ya existen antes de procesar
        /// </summary>
        public async Task<List<ResultadoBatchDto>> AcreditarDepositos(
            List<DepositoAcreditacionDto> depositosSeleccionados,
            string usuarioActual = "Sistema",
            bool esExcepcionScotiabank = false)
        {
            if (depositosSeleccionados == null || !depositosSeleccionados.Any())
                throw new ArgumentException("No se seleccionaron depósitos para acreditar");

            var depositosAcreditables = depositosSeleccionados.Where(d => d.HasCuentaAsignada && d.IdCuenta != 0).ToList();

            // Único bloque: sin acreditables (sin cuenta asignada o IdCuenta inválido) → warning y resultado sin procesar
            if (!depositosAcreditables.Any())
            {
                ServicioLog.instancia.WriteWarning(
                    "No hay depósitos acreditables (sin cuenta asignada o IdCuenta inválido). Total seleccionados: " + depositosSeleccionados.Count,
                    "ServicioAcreditacionManual | AcreditarDepositos");
                return new List<ResultadoBatchDto>
                {
                    new ResultadoBatchDto
                    {
                        Banco = null,
                        Mensaje = "No hay depósitos acreditables (sin cuenta asignada o IdCuenta inválido).",
                        Exitoso = false
                    }
                };
            }

            var correlacionId = Guid.NewGuid();
            
            ServicioLog.instancia.WriteInfo(
                $"INICIO BATCH ACREDITACIÓN MANUAL | Correlación: {correlacionId} | " +
                $"Total depósitos seleccionados: {depositosAcreditables.Count} | Usuario: {usuarioActual} | " +
                $"EsExcepcionScotiabank: {esExcepcionScotiabank}",
                "ServicioAcreditacionManual | AcreditarDepositos");

            // ✅ Verificar qué depósitos ya existen en BD (idempotencia)
            var depositosExistentes = await VerificarDepositosExistentes(depositosAcreditables);
            int totalExistentes = depositosExistentes.Count;
            
            // Filtrar depósitos nuevos (que NO existen)
            var depositosNuevos = depositosAcreditables
                .Where(d =>
                {
                    var monedaId = d.Moneda == VariablesGlobales.pesos ? 1 : 2;
                    var clave = (d.Codigo?.Trim(), (long)d.IdOperacion, monedaId, d.IdCuenta, d.MontoTotal, d.FechaDep.Date);
                    return !depositosExistentes.Contains(clave);
                })
                .ToList();

            int totalNuevos = depositosNuevos.Count;
            
            ServicioLog.instancia.WriteInfo(
                $"FILTRADO IDEMPOTENTE | Total seleccionados: {depositosAcreditables.Count} | " +
                $"Total existentes (skip): {totalExistentes} | Total nuevos a procesar: {totalNuevos} | " +
                $"Correlación: {correlacionId}",
                "ServicioAcreditacionManual | AcreditarDepositos");

            // Si no hay depósitos nuevos, retornar sin procesar
            if (!depositosNuevos.Any())
            {
                ServicioLog.instancia.WriteInfo(
                    $"FIN BATCH ACREDITACIÓN MANUAL (SIN PROCESAR) | Correlación: {correlacionId} | " +
                    $"Todos los depósitos ya estaban acreditados: {totalExistentes}",
                    "ServicioAcreditacionManual | AcreditarDepositos");
                
                // Retornar resultado vacío pero informativo
                return new List<ResultadoBatchDto>();
            }

            // Agrupar depósitos NUEVOS por banco (solo procesar los que no existen)
            var depositosPorBanco = depositosNuevos
                .GroupBy(d => d.Banco ?? "SIN_BANCO")
                .ToList();

            var resultados = new List<ResultadoBatchDto>();

            foreach (var grupoBanco in depositosPorBanco)
            {
                var bancoNombre = grupoBanco.Key;
                var depositosDelBanco = grupoBanco.ToList();

                var resultado = new ResultadoBatchDto
                {
                    Banco = bancoNombre,
                    TotalSeleccionados = depositosDelBanco.Count,
                    TotalOmitidos = depositosAcreditables
                        .Where(d => d.Banco == bancoNombre && depositosExistentes.Contains((
                            d.Codigo?.Trim(), 
                            (long)d.IdOperacion, 
                            d.Moneda == VariablesGlobales.pesos ? 1 : 2, 
                            d.IdCuenta, 
                            d.MontoTotal, 
                            d.FechaDep.Date)))
                        .Count()
                };

                try
                {
                    // Obtener banco
                    var banco = ServicioBanco.getInstancia().getByNombre(bancoNombre);
                    if (banco == null)
                    {
                        resultado.Exitoso = false;
                        resultado.Mensaje = $"Banco '{bancoNombre}' no encontrado";
                        resultado.TotalErrores = depositosDelBanco.Count;
                        resultados.Add(resultado);
                        continue;
                    }

                    resultado.IdBanco = banco.BancoId;

                    // ✅ Determinar si es excepción para este banco específico (solo Scotiabank)
                    bool esExcepcionParaEsteBanco = esExcepcionScotiabank && 
                        bancoNombre.Equals("Scotiabank", StringComparison.OrdinalIgnoreCase);

                    // Convertir depósitos NUEVOS a CuentaBuzon para usar el generador existente
                    var cuentasBuzones = await ConvertirDepositosACuentaBuzones(depositosDelBanco, esExcepcionParaEsteBanco);
                    
                    // depositosDelBanco ya está filtrado (solo nuevos), así que todos deben procesarse
                    var cuentasBuzonesFiltradas = cuentasBuzones;

                    // Generar archivo: usar el TipoAcreditacion de cada CuentaBuzon (viene de ObtenerCuentaBuzonPorId)
                    // para que P2P vaya a carpeta Punto a Punto y DiaADia a Día a Día (igual que los jobs).
                    try
                    {
                        var servicioCuentaBuzon = ServicioCuentaBuzon.getInstancia();
                        var porTipoAcreditacion = cuentasBuzonesFiltradas
                            .GroupBy(cb => (cb.Config?.TipoAcreditacion ?? string.Empty).Trim())
                            .Where(g => !string.IsNullOrEmpty(g.Key))
                            .ToList();
                        if (porTipoAcreditacion.Count == 0)
                        {
                            resultado.Exitoso = false;
                            resultado.Mensaje = "No se pudo determinar TipoAcreditacion para las cuentas (Config vacío o sin tipo).";
                            resultado.TotalErrores = depositosDelBanco.Count;
                            resultados.Add(resultado);
                            continue;
                        }
                        var resultadosGeneracion = new List<GeneracionArchivoBancoResult>();
                        foreach (var grupo in porTipoAcreditacion)
                        {
                            var tipoAcred = grupo.Key;
                            var lista = grupo.ToList();
                            resultadosGeneracion.Add(
                                await servicioCuentaBuzon.generarArchivoPorBanco(lista, banco, tipoAcred));
                        }

                        if (resultadosGeneracion.Any(r => r.RequiereAuditoriaEnvioFallido))
                        {
                            var motivoAuditoria = string.Join(" | ",
                                resultadosGeneracion.Where(r => r.RequiereAuditoriaEnvioFallido).Select(r => r.Motivo));
                            await ServicioAcreditacion.getInstancia().RegistrarSantanderPendienteAuditoriaPorFalloEnvioWs(
                                cuentasBuzonesFiltradas, "FALLIDO_WS", motivoAuditoria);

                            resultado.Exitoso = false;
                            resultado.Mensaje =
                                "Archivo generado en disco; el envío al Web Service de Santander falló. " +
                                "Se registró auditoría en AcreditacionDepositoSantanderPendiente (no se insertó en acreditaciones principales).";
                            resultado.TotalErrores = depositosDelBanco.Count;
                            resultados.Add(resultado);
                            ServicioLog.instancia.WriteWarning(
                                $"BATCH ACREDITACIÓN MANUAL | Envío WS Santander fallido | Banco: {bancoNombre} | {motivoAuditoria}",
                                "ServicioAcreditacionManual | AcreditarDepositos");
                            continue;
                        }

                        resultado.Exitoso = true;
                        resultado.Mensaje = "Archivo generado exitosamente";
                    }
                    catch (Exception exArchivo)
                    {
                        // Si falla la generación del archivo, NO insertamos nada
                        resultado.Exitoso = false;
                        resultado.Mensaje = $"Error al generar archivo: {exArchivo.Message}";
                        resultado.TotalErrores = depositosDelBanco.Count;
                        resultado.Errores.Add(exArchivo.Message);
                        resultados.Add(resultado);
                        ServicioLog.instancia.WriteLog(exArchivo, "Todos", 
                            $"ServicioAcreditacionManual | AcreditarDepositos | Banco: {bancoNombre}");
                        continue;
                    }

                    // Si el archivo se generó correctamente, insertar acreditaciones en BD
                    // Usar transacción para atomicidad
                    using var conn = new SqlConnection(_conexionTSD);
                    await conn.OpenAsync();
                    using var trans = conn.BeginTransaction();

                    try
                    {
                        int insertados = 0;
                        // ✅ Insertar solo depósitos nuevos (ya filtrados arriba)
                        foreach (var deposito in depositosDelBanco)
                        {
                            var acreditacion = new Acreditacion
                            {
                                IdBuzon = deposito.Codigo,
                                IdOperacion = deposito.IdOperacion,
                                Fecha = DateTime.Now,
                                IdBanco = banco.BancoId,
                                IdCuenta = deposito.IdCuenta,
                                Moneda = deposito.Moneda == VariablesGlobales.pesos ? 1 : 2,
                                No_Enviado = false,
                                Monto = deposito.MontoTotal,
                                FechaDepReal = deposito.FechaDep
                            };

                            // Usar el método insertar existente pero dentro de transacción
                            await InsertarAcreditacionEnTransaccion(acreditacion, conn, trans);
                            insertados++;

                            resultado.DepositosProcesados.Add(new DepositoProcesadoDto
                            {
                                IdOperacion = deposito.IdOperacion,
                                Codigo = deposito.Codigo,
                                Estado = "Insertado",
                                Mensaje = "Acreditación creada exitosamente",
                                Monto = deposito.MontoTotal
                            });
                        }

                        trans.Commit();
                        resultado.TotalInsertados = insertados;
                        resultado.Exitoso = true;
                        resultado.Mensaje = $"Procesados {insertados} depósitos nuevos exitosamente | " +
                                          $"{resultado.TotalOmitidos} ya existían (skip)";

                        ServicioLog.instancia.WriteInfo(
                            $"BATCH ACREDITACIÓN MANUAL COMPLETADO | Banco: {bancoNombre} | " +
                            $"Insertados: {insertados} | Omitidos (existentes): {resultado.TotalOmitidos} | " +
                            $"Correlación: {correlacionId}",
                            "ServicioAcreditacionManual | AcreditarDepositos");
                    }
                    catch (Exception exInsert)
                    {
                        trans.Rollback();
                        resultado.Exitoso = false;
                        resultado.Mensaje = $"Error al insertar acreditaciones: {exInsert.Message}";
                        resultado.TotalErrores = depositosDelBanco.Count;
                        resultado.Errores.Add(exInsert.Message);
                        ServicioLog.instancia.WriteLog(exInsert, "Todos",
                            $"ServicioAcreditacionManual | AcreditarDepositos | Banco: {bancoNombre}");
                    }
                }
                catch (Exception ex)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = $"Error general: {ex.Message}";
                    resultado.TotalErrores = depositosDelBanco.Count;
                    resultado.Errores.Add(ex.Message);
                    ServicioLog.instancia.WriteLog(ex, "Todos",
                        $"ServicioAcreditacionManual | AcreditarDepositos | Banco: {bancoNombre}");
                }

                resultados.Add(resultado);
            }

            int totalInsertadosFinal = resultados.Sum(r => r.TotalInsertados);
            int totalOmitidosFinal = resultados.Sum(r => r.TotalOmitidos);
            
            ServicioLog.instancia.WriteInfo(
                $"FIN BATCH ACREDITACIÓN MANUAL | Correlación: {correlacionId} | " +
                $"Total bancos procesados: {resultados.Count} | " +
                $"Bancos exitosos: {resultados.Count(r => r.Exitoso)} | " +
                $"Total insertados: {totalInsertadosFinal} | " +
                $"Total omitidos (existentes): {totalOmitidosFinal}",
                "ServicioAcreditacionManual | AcreditarDepositos");

            return resultados;
        }

        /// <summary>
        /// Convierte depósitos DTO a CuentaBuzon para usar con generadores existentes
        /// </summary>
        private async Task<List<CuentaBuzon>> ConvertirDepositosACuentaBuzones(
            List<DepositoAcreditacionDto> depositos, 
            bool esExcepcionScotiabank = false)
        {
            var cuentasBuzones = new List<CuentaBuzon>();

            // Agrupar por cuenta para crear CuentaBuzon
            var depositosPorCuenta = depositos.GroupBy(d => d.IdCuenta).ToList();

            foreach (var grupo in depositosPorCuenta)
            {
                var primerDep = grupo.First();
                
                // Obtener información completa de la cuenta desde BD
                var cuentaBuzon = await ObtenerCuentaBuzonPorId(primerDep.IdCuenta, primerDep.Codigo);
                if (cuentaBuzon == null)
                    continue;

                // ✅ Si es excepción para Scotiabank, marcar en Config
                if (esExcepcionScotiabank && cuentaBuzon.Config != null)
                {
                    cuentaBuzon.Config.EsExcepcionScotiabank = true;
                }

                // Agregar depósitos a la cuenta
                foreach (var depositoDto in grupo)
                {
                    var deposito = new Deposito
                    {
                        IdDeposito = depositoDto.IdDeposito,
                        IdOperacion = depositoDto.IdOperacion,
                        Codigo = depositoDto.Codigo,
                        Empresa = depositoDto.Empresa,
                        FechaDep = depositoDto.FechaDep,
                        Tipo = depositoDto.Tipo
                    };

                    // Agregar totales
                    var total = new Total
                    {
                        Divisa = depositoDto.Divisa,
                        ImporteTotal = (int)depositoDto.MontoTotal
                    };
                    deposito.Totales.Add(total);

                    cuentaBuzon.Depositos.Add(deposito);
                }

                cuentasBuzones.Add(cuentaBuzon);
            }

            return cuentasBuzones;
        }

        /// <summary>
        /// Obtiene CuentaBuzon completo por ID de cuenta y NC.
        /// Si hay varias filas en ConfiguracionAcreditacion para la misma (CuentasBuzonesId, NC) con distinto TipoAcreditacion,
        /// se toma la que corresponde a Punto a Punto (P2P), luego Tanda, luego Día a Día (DiaADia), para que la carpeta/ruta sea la correcta.
        /// </summary>
        private async Task<CuentaBuzon> ObtenerCuentaBuzonPorId(int idCuenta, string nc)
        {
            const string sql = @"
                SELECT 
                    cb.ID,
                    cb.BANCO,
                    c.BANCO as BANCOBUZON,
                    c.CIERRE,
                    cb.IDCLIENTE,
                    cb.CUENTA,
                    cb.MONEDA,
                    cb.EMPRESA,
                    c.SUCURSAL AS CIUDAD,
                    cb.SUCURSAL,
                    c.IDCC,
                    c.NN,
                    config.TipoAcreditacion AS CONFIGURACION
                FROM cuentasbuzones cb
                INNER JOIN ConfiguracionAcreditacion config ON config.CuentasBuzonesId = cb.ID
                INNER JOIN cc c ON c.NC = config.NC AND c.IDCLIENTE = cb.IDCLIENTE
                WHERE cb.ID = @idCuenta
                  AND config.NC = @nc
                ORDER BY CASE WHEN LTRIM(RTRIM(config.TipoAcreditacion)) = 'PuntoAPunto' THEN 0
                             WHEN LTRIM(RTRIM(config.TipoAcreditacion)) = 'Tanda' THEN 1
                             WHEN LTRIM(RTRIM(config.TipoAcreditacion)) = 'DiaADia' THEN 2
                             ELSE 3 END";

            using var conn = new SqlConnection(_conexionTSD);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idCuenta", idCuenta);
            cmd.Parameters.AddWithValue("@nc", nc.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var idccOrdinal = reader.GetOrdinal("IDCC");
            var cuentaBuzon = new CuentaBuzon
            {
                IdCuenta = reader.GetInt32(reader.GetOrdinal("ID")),
                NC = nc.Trim(),
                Banco = reader.GetString(reader.GetOrdinal("BANCO")),
                BancoBuzon = reader.IsDBNull(reader.GetOrdinal("BANCOBUZON")) ? null : reader.GetString(reader.GetOrdinal("BANCOBUZON")),
                Cierre = reader.IsDBNull(reader.GetOrdinal("CIERRE")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CIERRE")),
                IdCliente = reader.GetInt32(reader.GetOrdinal("IDCLIENTE")),
                Cuenta = reader.GetString(reader.GetOrdinal("CUENTA")),
                Moneda = reader.GetString(reader.GetOrdinal("MONEDA")),
                Empresa = reader.GetString(reader.GetOrdinal("EMPRESA")),
                Ciudad = reader.GetString(reader.GetOrdinal("CIUDAD")),
                SucursalCuenta = reader.GetString(reader.GetOrdinal("SUCURSAL")),
                IdReferenciaAlCliente = reader.IsDBNull(idccOrdinal) ? null : reader.GetString(idccOrdinal),
                NN = reader.GetString(reader.GetOrdinal("NN"))
            };

            cuentaBuzon.setDivisa();
            cuentaBuzon.setCashOffice();

            var configTipo = reader.GetString(reader.GetOrdinal("CONFIGURACION"));
            cuentaBuzon.Config = new ConfiguracionAcreditacion(configTipo);

            return cuentaBuzon;
        }

        /// <summary>
        /// Inserta acreditación dentro de una transacción
        /// </summary>
        private async Task InsertarAcreditacionEnTransaccion(
            Acreditacion a,
            SqlConnection conn,
            SqlTransaction trans)
        {
            if (a == null) return;

            DateTime fechaParaInsertar = a.FechaTanda != DateTime.MinValue ? a.FechaTanda : DateTime.Now;

            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
                // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
                var tableName = TableNameResolver.AcreditacionDeposito;
                TableNameResolver.ValidateTableName(tableName, "ServicioAcreditacionManual.InsertarAcreditacionEnTransaccion");

            cmd.CommandText = $@"
                IF NOT EXISTS (
                    SELECT 1
                    FROM {tableName}
                    WHERE IDBUZON = @IDBUZON
                      AND IDOPERACION = @IDOPERACION
                      AND MONEDA = @MONEDA
                      AND IDCUENTA = @IDCUENTA
                )
                BEGIN
                    INSERT INTO {tableName}
                    (IDBUZON, IDOPERACION, FECHA, IDBANCO, IDCUENTA, MONEDA, NO_ENVIADO, MONTO, FECHADEP)
                    VALUES
                    (@IDBUZON, @IDOPERACION, @FECHA, @IDBANCO, @IDCUENTA, @MONEDA, @NO_ENVIADO, @MONTO, @FECHADEPREAL);
                END";

            cmd.Parameters.AddWithValue("@IDBUZON", a.IdBuzon);
            cmd.Parameters.AddWithValue("@IDOPERACION", a.IdOperacion);
            cmd.Parameters.AddWithValue("@FECHA", fechaParaInsertar);
            cmd.Parameters.AddWithValue("@IDBANCO", (object)a.IdBanco ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IDCUENTA", (object)a.IdCuenta ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MONEDA", a.Moneda);
            cmd.Parameters.AddWithValue("@NO_ENVIADO", a.No_Enviado);
            cmd.Parameters.AddWithValue("@MONTO", a.Monto);
            cmd.Parameters.AddWithValue("@FECHADEPREAL",
                a.FechaDepReal != DateTime.MinValue ? (object)a.FechaDepReal : DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
