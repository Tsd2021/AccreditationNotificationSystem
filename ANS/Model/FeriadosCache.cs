using ANS.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ANS.Model
{
    /// <summary>
    /// Cache en memoria de fechas que son feriados activos.
    /// Sincronizado con BD al iniciar y tras cada CRUD (vía RefreshAsync).
    /// </summary>
    public sealed class FeriadosCache : IFeriadosProvider
    {
        private readonly Func<Task<HashSet<DateTime>>> _loader;
        private HashSet<DateTime> _fechas = new HashSet<DateTime>();
        private readonly object _lock = new object();

        public FeriadosCache(Func<Task<HashSet<DateTime>>> loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        public bool IsFeriadoActivo(DateTime fechaLocal)
        {
            var dateOnly = fechaLocal.Date;
            lock (_lock)
            {
                return _fechas.Contains(dateOnly);
            }
        }

        public async Task RefreshAsync()
        {
            var set = await _loader().ConfigureAwait(false);
            lock (_lock)
            {
                _fechas = set;
            }
        }
    }
}
