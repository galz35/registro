import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { importCenso, importCatalogo } from '../services/asistencia.api';
import { Upload, ChevronLeft, FileSpreadsheet } from 'lucide-react';

export default function ImportPage() {
  const navigate = useNavigate();
  const [tipo, setTipo] = useState<'censo' | 'catalogo'>('censo');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [notif, setNotif] = useState<{ msg: string; type: 'success' | 'error' } | null>(null);

  const show = (msg: string, type: 'success' | 'error' = 'success') => {
    setNotif({ msg, type });
    setTimeout(() => setNotif(null), 3000);
  };

  const handleFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setLoading(true);
    setResult(null);
    try {
      const res = tipo === 'censo' ? await importCenso(file) : await importCatalogo(file);
      setResult(res);
      show('Importación exitosa');
    } catch (err: any) {
      const msg = err?.response?.data?.message || 'Error al importar';
      setResult({ error: msg });
      show(msg, 'error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app-shell">
      <header className="page-header">
        <div className="page-header__inner">
          <button onClick={() => navigate('/')} className="toolbar-button">
            <ChevronLeft className="w-4 h-4" />
            Dashboard
          </button>
          <div>
            <div className="page-title">
              <Upload className="w-5 h-5" />
              <h1>Importar Excel</h1>
            </div>
            <p className="page-subtitle">Carga controlada de censo y catálogo</p>
          </div>
          <div />
        </div>
      </header>

      <main className="page-content">
        <section className="card mx-auto" style={{ maxWidth: 680 }}>
          <div className="card-header">
            <h2 className="section-title">
              <FileSpreadsheet className="w-4 h-4" />
              Archivo de importación
            </h2>
          </div>
          <div className="card-body">
            <div className="grid grid-cols-2 gap-3 mb-6">
              {(['censo', 'catalogo'] as const).map((t) => (
                <button
                  key={t}
                  onClick={() => { setTipo(t); setResult(null); }}
                  className="btn"
                  style={tipo === t ? { background: '#da121a', color: 'white' } : { background: '#f3f4f6', color: '#6b7280' }}
                >
                  {t === 'censo' ? 'Censo' : 'Catálogo'}
                </button>
              ))}
            </div>

            <label
              className="block cursor-pointer text-center"
              style={{
                border: '2px dashed #d1d5db',
                borderRadius: 'var(--radius-xl)',
                padding: 40,
                background: '#f8fafc',
                cursor: loading ? 'wait' : 'pointer',
              }}
            >
              {loading ? (
                <div>
                  <div className="animate-spin" style={{ width: 34, height: 34, border: '3px solid #e5e7eb', borderTopColor: '#da121a', borderRadius: '50%', margin: '0 auto 12px' }} />
                  <p className="m-0 text-sm font-bold" style={{ color: 'var(--text-secondary)' }}>Procesando...</p>
                </div>
              ) : (
                <>
                  <FileSpreadsheet className="w-10 h-10 mb-3 mx-auto" style={{ color: '#9ca3af' }} />
                  <p className="m-0 mb-1 text-sm font-bold" style={{ color: '#374151' }}>
                    Seleccione archivo .xlsx
                  </p>
                  <p className="m-0 text-xs" style={{ color: '#9ca3af' }}>
                    {tipo === 'censo' ? 'DATAS ANALIZADAS PADRE_HIJOS.xlsx' : 'SELLECCION DE JUGUETES 2026.xlsx'}
                  </p>
                  <input type="file" accept=".xlsx" style={{ display: 'none' }} onChange={handleFile} />
                </>
              )}
            </label>

            {result && !result.error && (
              <div className="mt-5 p-4 rounded-lg" style={{ background: '#d1fae5', border: '1px solid rgba(16,185,129,0.2)' }}>
                <p className="m-0 mb-1 font-bold" style={{ color: '#065f46' }}>Importación exitosa</p>
                {result.colaboradores !== undefined && <p className="m-0 text-sm" style={{ color: '#047857' }}>{result.colaboradores} colaboradores, {result.hijos} hijos</p>}
                {result.juguetes !== undefined && <p className="m-0 text-sm" style={{ color: '#047857' }}>{result.juguetes} juguetes importados</p>}
              </div>
            )}
            {result?.error && (
              <div className="mt-5 p-4 rounded-lg" style={{ background: '#fee2e2', border: '1px solid rgba(239,68,68,0.2)' }}>
                <p className="m-0 font-bold" style={{ color: '#991b1b' }}>{result.error}</p>
              </div>
            )}
          </div>
        </section>
      </main>

      {notif && <div className={`toast toast--${notif.type}`}>{notif.msg}</div>}
    </div>
  );
}
