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
                ORDER BY cb.EMPRESA, cb.MONEDA, cb.BANCO";

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

            while (await reader.ReadAsync())
            {
                resultado.Add(new EmpresaDto
                {
                    Empresa = reader.GetString(empresaOrd),
                    IdCuenta = reader.GetInt32(idCuentaOrd),
                    Cuenta = reader.GetString(cuentaOrd),
                    Moneda = reader.IsDBNull(monedaOrd) ? null : reader.GetString(monedaOrd),
                    Banco = reader.IsDBNull(bancoOrd) ? null : reader.GetString(bancoOrd),
                    IdBanco = reader.IsDBNull(idBancoOrd) ? 0 : reader.GetInt32(idBancoOrd)
                });
            }

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
        /// Mapea depósitos con su estado de acreditación (verifica si ya están acreditados)
        /// </summary>
        public async Task<List<DepositoAcreditacionDto>> MapearDepositosConEstadoAcreditado(List<DepositoAcreditacionDto> depositos)
        {
            if (depositos == null || !depositos.Any())
                return depositos;

            // Obtener todas las claves de acreditación existentes en batch
            var claves = depositos.Select(d => new
            {
                d.Codigo, // NC
                d.IdOperacion,
                d.IdCuenta,
                MonedaId = d.Moneda == VariablesGlobales.pesos ? 1 : 2
            }).ToList();

            if (!claves.Any())
                return depositos;

            // Consulta batch para obtener acreditaciones existentes
            // Usamos IN con múltiples condiciones en lugar de TVP para simplificar
            var condiciones = new List<string>();
            var parametros = new List<SqlParameter>();

            for (int i = 0; i < claves.Count; i++)
            {
                var clave = claves[i];
                condiciones.Add($"(IDBUZON = @nc{i} AND IDOPERACION = @idOp{i} AND IDCUENTA = @idCuenta{i} AND MONEDA = @moneda{i})");
                parametros.Add(new SqlParameter($"@nc{i}", clave.Codigo?.Trim()));
                parametros.Add(new SqlParameter($"@idOp{i}", (long)clave.IdOperacion));
                parametros.Add(new SqlParameter($"@idCuenta{i}", clave.IdCuenta));
                parametros.Add(new SqlParameter($"@moneda{i}", clave.MonedaId));
            }

            if (!condiciones.Any())
                return depositos;

                // ✅ Usar TableNameResolver para obtener nombre de tabla según RuntimeMode
                var tableName = TableNameResolver.AcreditacionDeposito;
                TableNameResolver.ValidateTableName(tableName, "ServicioAcreditacionManual.MapearDepositosConEstadoAcreditado");

            var sql = $@"
                SELECT 
                    IDBUZON,
                    IDOPERACION,
                    IDCUENTA,
                    MONEDA,
                    MONTO,
                    FECHA
                FROM {tableName}
                WHERE ({string.Join(" OR ", condiciones)})";

            var acreditacionesExistentes = new Dictionary<(string NC, long IdOp, int IdCuenta, int Moneda), (double Monto, DateTime Fecha)>();

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
            int cuentaOrd = reader.GetOrdinal("IDCUENTA");
            int monedaOrd = reader.GetOrdinal("MONEDA");
            int montoOrd = reader.GetOrdinal("MONTO");
            int fechaOrd = reader.GetOrdinal("FECHA");

            while (await reader.ReadAsync())
            {
                var nc = reader.GetString(ncOrd)?.Trim();
                var idOp = reader.GetInt64(opOrd);
                var idCuenta = reader.GetInt32(cuentaOrd);
                var moneda = reader.GetInt32(monedaOrd);
                var monto = reader.GetDouble(montoOrd);
                var fecha = reader.GetDateTime(fechaOrd);

                acreditacionesExistentes[(nc, idOp, idCuenta, moneda)] = (monto, fecha);
            }

            // Mapear estado a cada depósito
            foreach (var deposito in depositos)
            {
                var monedaId = deposito.Moneda == VariablesGlobales.pesos ? 1 : 2;
                var clave = (deposito.Codigo?.Trim(), (long)deposito.IdOperacion, deposito.IdCuenta, monedaId);

                if (acreditacionesExistentes.TryGetValue(clave, out var acreditacion))
                {
                    deposito.IsAcreditado = true;
                    deposito.FechaAcreditacion = acreditacion.Fecha;
                    deposito.MontoAcreditado = acreditacion.Monto;

                    // Advertencia si el monto es diferente
                    if (Math.Abs(deposito.MontoTotal - acreditacion.Monto) > 0.01)
                    {
                        ServicioLog.instancia.WriteWarning(
                            $"Depósito ya acreditado con monto diferente | " +
                            $"NC: {deposito.Codigo} | IDOperacion: {deposito.IdOperacion} | " +
                            $"Monto actual: {deposito.MontoTotal} | Monto acreditado: {acreditacion.Monto}",
                            "ServicioAcreditacionManual | MapearDepositosConEstadoAcreditado");
                    }
                }
            }

            return depositos;
        }

        /// <summary>
        /// Acredita múltiples depósitos (genera archivos por banco + inserta en BD de forma transaccional)
        /// </summary>
        public async Task<List<ResultadoBatchDto>> AcreditarDepositos(
            List<DepositoAcreditacionDto> depositosSeleccionados,
            string usuarioActual = "Sistema")
        {
            if (depositosSeleccionados == null || !depositosSeleccionados.Any())
                throw new ArgumentException("No se seleccionaron depósitos para acreditar");

            var correlacionId = Guid.NewGuid();
            ServicioLog.instancia.WriteInfo(
                $"INICIO BATCH ACREDITACIÓN MANUAL | Correlación: {correlacionId} | " +
                $"Total depósitos seleccionados: {depositosSeleccionados.Count} | Usuario: {usuarioActual}",
                "ServicioAcreditacionManual | AcreditarDepositos");

            // Agrupar depósitos por banco
            var depositosPorBanco = depositosSeleccionados
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
                    TotalSeleccionados = depositosDelBanco.Count
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

                    // Convertir depósitos a CuentaBuzon para usar el generador existente
                    var cuentasBuzones = await ConvertirDepositosACuentaBuzones(depositosDelBanco);

                    // Verificar cuáles ya están acreditados
                    var depositosConEstado = await MapearDepositosConEstadoAcreditado(depositosDelBanco);
                    var depositosParaAcreditar = depositosConEstado
                        .Where(d => !d.IsAcreditado)
                        .ToList();

                    resultado.TotalOmitidos = depositosConEstado.Count - depositosParaAcreditar.Count;

                    if (!depositosParaAcreditar.Any())
                    {
                        resultado.Exitoso = true;
                        resultado.Mensaje = "Todos los depósitos seleccionados ya estaban acreditados";
                        resultados.Add(resultado);
                        continue;
                    }

                    // Filtrar cuentasBuzones para solo incluir depósitos no acreditados
                    var cuentasBuzonesFiltradas = cuentasBuzones
                        .Where(cb => depositosParaAcreditar.Any(d => 
                            d.IdOperacion == cb.Depositos.FirstOrDefault()?.IdOperacion))
                        .ToList();

                    // Generar archivo (en memoria/temporal primero)
                    string rutaArchivo = null;
                    string nombreArchivo = null;

                    try
                    {
                        // Usar el generador existente - asumimos que genera archivos en disco
                        // Para acreditación manual, usamos tipo "DiaADia" por defecto
                        await ServicioCuentaBuzon.getInstancia()
                            .generarArchivoPorBanco(cuentasBuzonesFiltradas, banco, VariablesGlobales.diaxdia);

                        // El generador ya guarda el archivo, pero necesitamos obtener la ruta
                        // Por ahora, marcamos como exitoso
                        resultado.Exitoso = true;
                        resultado.Mensaje = "Archivo generado exitosamente";
                    }
                    catch (Exception exArchivo)
                    {
                        // Si falla la generación del archivo, NO insertamos nada
                        resultado.Exitoso = false;
                        resultado.Mensaje = $"Error al generar archivo: {exArchivo.Message}";
                        resultado.TotalErrores = depositosParaAcreditar.Count;
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
                        foreach (var deposito in depositosParaAcreditar)
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
                        resultado.Mensaje = $"Procesados {insertados} depósitos exitosamente";

                        ServicioLog.instancia.WriteInfo(
                            $"BATCH ACREDITACIÓN MANUAL COMPLETADO | Banco: {bancoNombre} | " +
                            $"Insertados: {insertados} | Omitidos: {resultado.TotalOmitidos} | " +
                            $"Correlación: {correlacionId}",
                            "ServicioAcreditacionManual | AcreditarDepositos");
                    }
                    catch (Exception exInsert)
                    {
                        trans.Rollback();
                        resultado.Exitoso = false;
                        resultado.Mensaje = $"Error al insertar acreditaciones: {exInsert.Message}";
                        resultado.TotalErrores = depositosParaAcreditar.Count;
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

            ServicioLog.instancia.WriteInfo(
                $"FIN BATCH ACREDITACIÓN MANUAL | Correlación: {correlacionId} | " +
                $"Total bancos procesados: {resultados.Count} | " +
                $"Bancos exitosos: {resultados.Count(r => r.Exitoso)}",
                "ServicioAcreditacionManual | AcreditarDepositos");

            return resultados;
        }

        /// <summary>
        /// Convierte depósitos DTO a CuentaBuzon para usar con generadores existentes
        /// </summary>
        private async Task<List<CuentaBuzon>> ConvertirDepositosACuentaBuzones(List<DepositoAcreditacionDto> depositos)
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
        /// Obtiene CuentaBuzon completo por ID de cuenta y NC
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
                  AND config.NC = @nc";

            using var conn = new SqlConnection(_conexionTSD);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idCuenta", idCuenta);
            cmd.Parameters.AddWithValue("@nc", nc.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

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
