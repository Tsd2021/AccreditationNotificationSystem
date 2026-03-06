using ANS.Model.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ANS.Model.Services
{
    /// <summary>
    /// Servicio TAAS: CRUD Feriados y Tipos de Feriado, validación Dias por año, y soporte para cache.
    /// </summary>
    public class ServicioFeriadosTAAS
    {
        public const string GrupoBBVA = "GrupoTrabajoBBVA";

        private static ServicioFeriadosTAAS? _instanciaParaUI;

        /// <summary>Instancia con cache para la UI WPF (se asigna desde App al iniciar).</summary>
        public static ServicioFeriadosTAAS GetInstancia()
        {
            if (_instanciaParaUI == null)
                throw new InvalidOperationException("ServicioFeriadosTAAS no inicializado. Debe configurarse desde App al iniciar.");
            return _instanciaParaUI;
        }

        public static void SetInstanciaParaUI(ServicioFeriadosTAAS servicio) => _instanciaParaUI = servicio;

        private readonly string _conexionTsd;
        private readonly IFeriadosProvider? _cacheToRefresh;

        public ServicioFeriadosTAAS(string conexionTsd, IFeriadosProvider? cacheToRefresh = null)
        {
            _conexionTsd = conexionTsd ?? throw new ArgumentNullException(nameof(conexionTsd));
            _cacheToRefresh = cacheToRefresh;
        }

        private async Task NotifyCacheRefreshAsync()
        {
            if (_cacheToRefresh != null)
                await _cacheToRefresh.RefreshAsync().ConfigureAwait(false);
        }

        // ---------- Fechas activas (para el cache) ----------
        /// <summary>
        /// Devuelve las fechas (solo día) que son feriados activos. Usado por FeriadosCache.
        /// </summary>
        public async Task<HashSet<DateTime>> GetFechasActivasAsync()
        {
            var set = new HashSet<DateTime>();
            const string sql = "SELECT Feriado FROM FeriadosTAAS WHERE Activo = 1";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            int ord = reader.GetOrdinal("Feriado");
            while (await reader.ReadAsync().ConfigureAwait(false))
                set.Add(reader.GetDateTime(ord).Date);
            return set;
        }

        // ---------- Tipos de Feriado ----------
        public async Task<List<TipoFeriadoTAAS>> ListTiposAsync()
        {
            var list = new List<TipoFeriadoTAAS>();
            const string sql = "SELECT Id, TipoFeriado, Dias FROM TipoFeriadosTAAS ORDER BY TipoFeriado";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
                list.Add(new TipoFeriadoTAAS
                {
                    Id = reader.GetInt32(0),
                    TipoFeriado = reader.GetString(1),
                    Dias = reader.GetInt32(2)
                });
            return list;
        }

        public async Task<TipoFeriadoTAAS?> GetTipoAsync(int id)
        {
            const string sql = "SELECT Id, TipoFeriado, Dias FROM TipoFeriadosTAAS WHERE Id = @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (!await reader.ReadAsync().ConfigureAwait(false)) return null;
            return new TipoFeriadoTAAS
            {
                Id = reader.GetInt32(0),
                TipoFeriado = reader.GetString(1),
                Dias = reader.GetInt32(2)
            };
        }

        /// <summary>
        /// Crea tipo. Valida Dias > 0 y que no exista ya un tipo con el mismo nombre (TipoFeriado).
        /// </summary>
        public async Task<(int Id, string? Error)> CreateTipoAsync(string tipoFeriado, int dias)
        {
            if (string.IsNullOrWhiteSpace(tipoFeriado))
                return (0, "El nombre del tipo es obligatorio.");
            if (dias <= 0)
                return (0, "Dias debe ser mayor a 0.");
            var nombre = tipoFeriado.Trim();
            const string checkDup = "SELECT COUNT(1) FROM TipoFeriadosTAAS WHERE LOWER(LTRIM(RTRIM(TipoFeriado))) = LOWER(@Nombre)";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = new SqlCommand(checkDup, conn))
            {
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                var n = (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                if (n > 0)
                    return (0, "Ya existe un tipo de feriado con ese nombre.");
            }
            const string sql = "INSERT INTO TipoFeriadosTAAS (TipoFeriado, Dias) OUTPUT INSERTED.Id VALUES (@TipoFeriado, @Dias)";
            using var ins = new SqlCommand(sql, conn);
            ins.Parameters.AddWithValue("@TipoFeriado", nombre);
            ins.Parameters.AddWithValue("@Dias", dias);
            var id = (int)await ins.ExecuteScalarAsync().ConfigureAwait(false);
            return (id, null);
        }

        /// <summary>
        /// Actualiza tipo. Valida Dias > 0 y que no exista otro tipo con el mismo nombre (TipoFeriado).
        /// </summary>
        public async Task<string?> UpdateTipoAsync(int id, string tipoFeriado, int dias)
        {
            if (dias <= 0)
                return "Dias debe ser mayor a 0.";
            var nombre = (tipoFeriado ?? "").Trim();
            const string checkDup = "SELECT COUNT(1) FROM TipoFeriadosTAAS WHERE LOWER(LTRIM(RTRIM(TipoFeriado))) = LOWER(@Nombre) AND Id <> @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = new SqlCommand(checkDup, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                var n = (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                if (n > 0)
                    return "Ya existe otro tipo de feriado con ese nombre.";
            }
            const string sql = "UPDATE TipoFeriadosTAAS SET TipoFeriado = @TipoFeriado, Dias = @Dias WHERE Id = @Id";
            using var upd = new SqlCommand(sql, conn);
            upd.Parameters.AddWithValue("@Id", id);
            upd.Parameters.AddWithValue("@TipoFeriado", nombre);
            upd.Parameters.AddWithValue("@Dias", dias);
            var rows = await upd.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (rows == 0) return "Tipo no encontrado.";
            return null;
        }

        /// <summary>
        /// Borra tipo. Si está en uso por FeriadosTAAS, devuelve error.
        /// </summary>
        public async Task<string?> DeleteTipoAsync(int id)
        {
            const string check = "SELECT COUNT(1) FROM FeriadosTAAS WHERE IdTipoFeriado = @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = new SqlCommand(check, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                var count = (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                if (count > 0)
                    return "No se puede eliminar el tipo: está en uso por uno o más feriados.";
            }
            const string sql = "DELETE FROM TipoFeriadosTAAS WHERE Id = @Id";
            using var del = new SqlCommand(sql, conn);
            del.Parameters.AddWithValue("@Id", id);
            var rows = await del.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (rows == 0) return "Tipo no encontrado.";
            return null;
        }

        // ---------- Feriados ----------
        public async Task<List<FeriadoTAAS>> ListFeriadosAsync(int? anio = null)
        {
            var list = new List<FeriadoTAAS>();
            string sql = "SELECT f.Id, f.Feriado, f.Activo, f.IdTipoFeriado FROM FeriadosTAAS f";
            if (anio.HasValue)
                sql += " WHERE YEAR(f.Feriado) = @Anio";
            sql += " ORDER BY f.Feriado DESC";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            if (anio.HasValue)
                cmd.Parameters.AddWithValue("@Anio", anio.Value);
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
                list.Add(new FeriadoTAAS
                {
                    Id = reader.GetInt32(0),
                    Feriado = reader.GetDateTime(1),
                    Activo = reader.GetBoolean(2),
                    IdTipoFeriado = reader.GetInt32(3)
                });
            return list;
        }

        public async Task<FeriadoTAAS?> GetFeriadoAsync(int id)
        {
            const string sql = "SELECT Id, Feriado, Activo, IdTipoFeriado FROM FeriadosTAAS WHERE Id = @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (!await reader.ReadAsync().ConfigureAwait(false)) return null;
            return new FeriadoTAAS
            {
                Id = reader.GetInt32(0),
                Feriado = reader.GetDateTime(1),
                Activo = reader.GetBoolean(2),
                IdTipoFeriado = reader.GetInt32(3)
            };
        }

        /// <summary>
        /// Cuenta feriados activos del mismo tipo en el mismo año (para validar Dias).
        /// Excluye opcionalmente un Id (para UPDATE).
        /// </summary>
        private async Task<int> CountActivosMismoTipoYAnioAsync(int idTipoFeriado, int anio, int? excluirFeriadoId = null)
        {
            string sql = @"SELECT COUNT(1) FROM FeriadosTAAS 
WHERE IdTipoFeriado = @IdTipo AND YEAR(Feriado) = @Anio AND Activo = 1";
            if (excluirFeriadoId.HasValue)
                sql += " AND Id <> @ExcluirId";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IdTipo", idTipoFeriado);
            cmd.Parameters.AddWithValue("@Anio", anio);
            if (excluirFeriadoId.HasValue)
                cmd.Parameters.AddWithValue("@ExcluirId", excluirFeriadoId.Value);
            return (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
        }

        /// <summary>
        /// Valida regla Dias: count_activos + 1 <= Dias del tipo. Devuelve mensaje de error o null.
        /// </summary>
        private async Task<string?> ValidarDiasPorAnioAsync(int idTipoFeriado, DateTime feriado, int? excluirFeriadoId = null)
        {
            var tipo = await GetTipoAsync(idTipoFeriado).ConfigureAwait(false);
            if (tipo == null) return "Tipo de feriado no encontrado.";
            int anio = feriado.Year;
            int count = await CountActivosMismoTipoYAnioAsync(idTipoFeriado, anio, excluirFeriadoId).ConfigureAwait(false);
            if (count + 1 > tipo.Dias)
                return $"Se excede la cantidad máxima de días para este tipo en el año {anio}. Máximo permitido: {tipo.Dias}, actual (activos): {count}.";
            return null;
        }

        /// <summary>
        /// Crea feriado. Valida: fecha válida, Dias por año, duplicado por fecha.
        /// </summary>
        public async Task<(int Id, string? Error)> CreateFeriadoAsync(DateTime feriado, bool activo, int idTipoFeriado)
        {
            var fecha = feriado.Date;
            if (fecha.Year < 1900 || fecha.Year > 2100)
                return (0, "Fecha fuera de rango válido.");
            var errDias = await ValidarDiasPorAnioAsync(idTipoFeriado, fecha).ConfigureAwait(false);
            if (errDias != null) return (0, errDias);
            const string checkDup = "SELECT COUNT(1) FROM FeriadosTAAS WHERE Feriado = @Feriado";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = new SqlCommand(checkDup, conn))
            {
                cmd.Parameters.AddWithValue("@Feriado", fecha);
                var n = (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                if (n > 0) return (0, "Ya existe un feriado para esa fecha.");
            }
            const string sql = "INSERT INTO FeriadosTAAS (Feriado, Activo, IdTipoFeriado) OUTPUT INSERTED.Id VALUES (@Feriado, @Activo, @IdTipoFeriado)";
            using var ins = new SqlCommand(sql, conn);
            ins.Parameters.AddWithValue("@Feriado", fecha);
            ins.Parameters.AddWithValue("@Activo", activo);
            ins.Parameters.AddWithValue("@IdTipoFeriado", idTipoFeriado);
            var id = (int)await ins.ExecuteScalarAsync().ConfigureAwait(false);
            await NotifyCacheRefreshAsync().ConfigureAwait(false);
            return (id, null);
        }

        /// <summary>
        /// Actualiza feriado. Valida Dias por año y duplicado (si cambia fecha).
        /// </summary>
        public async Task<string?> UpdateFeriadoAsync(int id, DateTime feriado, bool activo, int idTipoFeriado)
        {
            var fecha = feriado.Date;
            if (fecha.Year < 1900 || fecha.Year > 2100)
                return "Fecha fuera de rango válido.";
            var errDias = await ValidarDiasPorAnioAsync(idTipoFeriado, fecha, id).ConfigureAwait(false);
            if (errDias != null) return errDias;
            const string checkDup = "SELECT COUNT(1) FROM FeriadosTAAS WHERE Feriado = @Feriado AND Id <> @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using (var cmd = new SqlCommand(checkDup, conn))
            {
                cmd.Parameters.AddWithValue("@Feriado", fecha);
                cmd.Parameters.AddWithValue("@Id", id);
                var n = (int)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                if (n > 0) return "Ya existe otro feriado para esa fecha.";
            }
            const string sql = "UPDATE FeriadosTAAS SET Feriado = @Feriado, Activo = @Activo, IdTipoFeriado = @IdTipoFeriado WHERE Id = @Id";
            using var upd = new SqlCommand(sql, conn);
            upd.Parameters.AddWithValue("@Id", id);
            upd.Parameters.AddWithValue("@Feriado", fecha);
            upd.Parameters.AddWithValue("@Activo", activo);
            upd.Parameters.AddWithValue("@IdTipoFeriado", idTipoFeriado);
            var rows = await upd.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (rows == 0) return "Feriado no encontrado.";
            await NotifyCacheRefreshAsync().ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Elimina feriado por Id.
        /// </summary>
        public async Task<string?> DeleteFeriadoAsync(int id)
        {
            const string sql = "DELETE FROM FeriadosTAAS WHERE Id = @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (rows == 0) return "Feriado no encontrado.";
            await NotifyCacheRefreshAsync().ConfigureAwait(false);
            return null;
        }

        /// <summary>
        /// Cambia Activo del feriado. Valida Dias si se activa (count + 1 <= Dias).
        /// </summary>
        public async Task<string?> ToggleActivoAsync(int id)
        {
            var f = await GetFeriadoAsync(id).ConfigureAwait(false);
            if (f == null) return "Feriado no encontrado.";
            bool nuevoActivo = !f.Activo;
            if (nuevoActivo)
            {
                var err = await ValidarDiasPorAnioAsync(f.IdTipoFeriado, f.Feriado, id).ConfigureAwait(false);
                if (err != null) return err;
            }
            const string sql = "UPDATE FeriadosTAAS SET Activo = @Activo WHERE Id = @Id";
            using var conn = new SqlConnection(_conexionTsd);
            await conn.OpenAsync().ConfigureAwait(false);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Activo", nuevoActivo);
            var rows = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            if (rows == 0) return "Feriado no encontrado.";
            await NotifyCacheRefreshAsync().ConfigureAwait(false);
            return null;
        }
    }
}
