import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { importCenso, importCatalogo, validateCenso, validateCatalogo, downloadTemplate } from '../services/asistencia.api';
import { Upload, ChevronLeft, FileSpreadsheet, Download, AlertTriangle, CheckCircle, XCircle } from 'lucide-react';
import { toast } from '../utils/toast';

export default function ImportPage() {
  const navigate = useNavigate();
  const [tipo, setTipo] = useState<'censo' | 'catalogo'>('censo');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [preview, setPreview] = useState<any>(null);
  const [file, setFile] = useState<File | null>(null);

  const show = (msg: string, type: 'success' | 'error' = 'success') => toast(msg, type);

  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    setFile(f);
    setResult(null);
    setLoading(true);
    try {
      const res = tipo === 'censo' ? await validateCenso(f) : await validateCatalogo(f);
      setPreview(res);
    } catch (err: any) {
      const msg = err?.response?.data?.message || 'Error al validar';
      setPreview({ error: msg });
      show(msg, 'error');
    } finally { setLoading(false); }
  };

  const handleApply = async () => {
    if (!file) return;
    setLoading(true);
    try {
      const res = tipo === 'censo' ? await importCenso(file) : await importCatalogo(file);
      setResult(res);
      setPreview(null);
      setFile(null);
      show('Importación exitosa');
    } catch (err: any) {
      const msg = err?.response?.data?.message || 'Error al importar';
      setResult({ error: msg });
      show(msg, 'error');
    } finally { setLoading(false); }
  };

  const limpiar = () => { setFile(null); setPreview(null); setResult(null); };

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
        <section className="card mx-auto" style={{ maxWidth: 780 }}>
          <div className="card-header">
            <h2 className="section-title">
              <FileSpreadsheet className="w-4 h-4" />
              Archivo de importación
            </h2>
          </div>
          <div className="card-body">
            {/* Tipo selector + template download */}
            <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
              {(['censo', 'catalogo'] as const).map((t) => (
                <button key={t} onClick={() => { setTipo(t); limpiar(); }}
                  className="btn" style={{ flex: 1,
                    ...(tipo === t ? { background: '#da121a', color: 'white' } : { background: '#f3f4f6', color: '#6b7280' })
                  }}>
                  {t === 'censo' ? 'Censo' : 'Catálogo'}
                </button>
              ))}
              <button onClick={() => downloadTemplate(tipo)}
                style={{ background: '#1e40af', color: 'white', border: 'none', borderRadius: 8, padding: '8px 16px', fontWeight: 600, fontSize: 12, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6, whiteSpace: 'nowrap' }}>
                <Download className="w-4 h-4" /> Plantilla
              </button>
            </div>

            {/* File upload area */}
            {!preview && !result && (
              <label className="block cursor-pointer text-center"
                style={{ border: '2px dashed #d1d5db', borderRadius: 16, padding: 36, background: '#f8fafc', cursor: loading ? 'wait' : 'pointer', display: 'block' }}>
                {loading ? (
                  <div>
                    <div className="animate-spin" style={{ width: 34, height: 34, border: '3px solid #e5e7eb', borderTopColor: '#da121a', borderRadius: '50%', margin: '0 auto 12px' }} />
                    <p style={{ margin: 0, fontSize: 13, fontWeight: 700, color: '#6b7280' }}>Validando...</p>
                  </div>
                ) : (
                  <>
                    <FileSpreadsheet className="w-10 h-10" style={{ margin: '0 auto 12px', color: '#9ca3af', display: 'block' }} />
                    <p style={{ margin: '0 0 4px', fontSize: 13, fontWeight: 700, color: '#374151' }}>Seleccione archivo .xlsx</p>
                    <p style={{ margin: 0, fontSize: 11, color: '#9ca3af' }}>
                      {tipo === 'censo' ? 'DATAS ANALIZADAS PADRE_HIJOS.xlsx' : 'SELLECCION DE JUGUETES 2026.xlsx'}
                    </p>
                    <input type="file" accept=".xlsx" style={{ display: 'none' }} onChange={handleFileSelect} />
                  </>
                )}
              </label>
            )}

            {/* Preview */}
            {preview && !preview.error && (
              <div style={{ marginTop: 16 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                  <h3 style={{ margin: 0, fontSize: 14, fontWeight: 700, display: 'flex', alignItems: 'center', gap: 6 }}>
                    <FileSpreadsheet className="w-4 h-4" /> Vista previa
                  </h3>
                  <div style={{ display: 'flex', gap: 8 }}>
                    <button onClick={limpiar} style={{ background: '#f3f4f6', border: 'none', borderRadius: 6, padding: '6px 14px', fontWeight: 600, fontSize: 12, cursor: 'pointer' }}>Cancelar</button>
                    <button onClick={handleApply} disabled={loading || preview.resumen?.erroresBloqueantes > 0}
                      style={{ background: preview.resumen?.erroresBloqueantes > 0 ? '#d1d5db' : '#da121a', color: 'white', border: 'none', borderRadius: 6, padding: '6px 14px', fontWeight: 700, fontSize: 12, cursor: preview.resumen?.erroresBloqueantes > 0 ? 'not-allowed' : 'pointer', display: 'flex', alignItems: 'center', gap: 6 }}>
                      {loading ? <div className="animate-spin" style={{ width: 14, height: 14, border: '2px solid rgba(255,255,255,0.3)', borderTopColor: 'white', borderRadius: '50%' }} /> : <Upload className="w-4 h-4" />}
                      Procesar
                    </button>
                  </div>
                </div>

                {/* Summary */}
                <div style={{ display: 'flex', gap: 12, marginBottom: 12, flexWrap: 'wrap' }}>
                  {preview.resumen && Object.entries(preview.resumen).map(([k, v]) => (
                    <div key={k} style={{ padding: '8px 14px', borderRadius: 8, background: '#f8fafc', border: '1px solid #e5e7eb', fontSize: 12 }}>
                      <div style={{ fontWeight: 700, color: '#374151', textTransform: 'capitalize' }}>{k.replace(/([A-Z])/g, ' $1').trim()}</div>
                      <div style={{ color: '#da121a', fontWeight: 800, fontSize: 18 }}>{String(v)}</div>
                    </div>
                  ))}
                </div>

                {/* Errors */}
                {preview.errores && preview.errores.length > 0 && (
                  <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fef2f2', border: '1px solid #fecaca' }}>
                    <p style={{ margin: '0 0 8px', fontWeight: 700, fontSize: 12, color: '#991b1b', display: 'flex', alignItems: 'center', gap: 4 }}>
                      <XCircle className="w-4 h-4" /> {preview.errores.length} error(es)
                    </p>
                    <div style={{ maxHeight: 150, overflowY: 'auto', fontSize: 11 }}>
                      {preview.errores.slice(0, 20).map((e: any, i: number) => (
                        <p key={i} style={{ margin: '2px 0', color: '#b91c1c' }}>Fila {e.fila}: {e.mensaje}</p>
                      ))}
                    </div>
                  </div>
                )}

                {/* Duplicates (censo) */}
                {preview.duplicados && preview.duplicados.length > 0 && (
                  <div style={{ marginBottom: 12, padding: 12, borderRadius: 8, background: '#fefce8', border: '1px solid #fde68a' }}>
                    <p style={{ margin: '0 0 8px', fontWeight: 700, fontSize: 12, color: '#92400e', display: 'flex', alignItems: 'center', gap: 4 }}>
                      <AlertTriangle className="w-4 h-4" /> {preview.duplicados.length} duplicado(s)
                    </p>
                    <div style={{ maxHeight: 120, overflowY: 'auto', fontSize: 11 }}>
                      {preview.duplicados.map((d: any, i: number) => (
                        <p key={i} style={{ margin: '2px 0', color: '#92400e' }}>Fila {d.fila} (original fila {d.filaOriginal}): {d.nombreHijo}</p>
                      ))}
                    </div>
                  </div>
                )}

                {/* Preview rows */}
                {preview.juguetes && preview.juguetes.length > 0 && (
                  <div style={{ overflowX: 'auto', border: '1px solid #e5e7eb', borderRadius: 8, fontSize: 12 }}>
                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                      <thead><tr style={{ background: '#f8fafc' }}>
                        <th style={{ padding: '6px 10px', textAlign: 'left', fontWeight: 700, color: '#6b7280' }}>#</th>
                        <th style={{ padding: '6px 10px', textAlign: 'left', fontWeight: 700, color: '#6b7280' }}>Categoría</th>
                        <th style={{ padding: '6px 10px', textAlign: 'left', fontWeight: 700, color: '#6b7280' }}>Género</th>
                        <th style={{ padding: '6px 10px', textAlign: 'left', fontWeight: 700, color: '#6b7280' }}>Juguete</th>
                        <th style={{ padding: '6px 10px', textAlign: 'center', fontWeight: 700, color: '#6b7280' }}>Stock</th>
                      </tr></thead>
                      <tbody>
                        {preview.juguetes.map((j: any, i: number) => (
                          <tr key={i} style={{ borderBottom: '1px solid #f3f4f6' }}>
                            <td style={{ padding: '6px 10px', color: '#9ca3af' }}>{i + 1}</td>
                            <td style={{ padding: '6px 10px' }}>{j.categoria}</td>
                            <td style={{ padding: '6px 10px' }}>{j.genero}</td>
                            <td style={{ padding: '6px 10px', fontWeight: 600 }}>{j.nombreJuguete}</td>
                            <td style={{ padding: '6px 10px', textAlign: 'center' }}>{j.stock}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}

            {/* Preview error */}
            {preview?.error && (
              <div style={{ marginTop: 16, padding: 16, borderRadius: 8, background: '#fee2e2', border: '1px solid #fecaca' }}>
                <p style={{ margin: 0, fontWeight: 700, color: '#991b1b', display: 'flex', alignItems: 'center', gap: 6 }}>
                  <XCircle className="w-4 h-4" /> {preview.error}
                </p>
                <button onClick={limpiar} style={{ marginTop: 8, background: '#dc2626', color: 'white', border: 'none', borderRadius: 6, padding: '6px 14px', fontWeight: 600, fontSize: 12, cursor: 'pointer' }}>Intentar de nuevo</button>
              </div>
            )}

            {/* Success result */}
            {result && !result.error && (
              <div style={{ marginTop: 16, padding: 16, borderRadius: 8, background: '#d1fae5', border: '1px solid #a7f3d0' }}>
                <p style={{ margin: '0 0 8px', fontWeight: 700, color: '#065f46', display: 'flex', alignItems: 'center', gap: 6 }}>
                  <CheckCircle className="w-5 h-5" /> Importación exitosa
                </p>
                {result.colaboradores !== undefined && <p style={{ margin: '2px 0', fontSize: 13, color: '#047857' }}>{result.colaboradores} colaboradores, {result.hijos} hijos</p>}
                {result.juguetes !== undefined && <p style={{ margin: '2px 0', fontSize: 13, color: '#047857' }}>{result.juguetes} juguetes importados</p>}
                <button onClick={limpiar} style={{ marginTop: 8, background: '#10b981', color: 'white', border: 'none', borderRadius: 6, padding: '6px 14px', fontWeight: 600, fontSize: 12, cursor: 'pointer' }}>Importar otro archivo</button>
              </div>
            )}
            {result?.error && (
              <div style={{ marginTop: 16, padding: 16, borderRadius: 8, background: '#fee2e2', border: '1px solid #fecaca' }}>
                <p style={{ margin: 0, fontWeight: 700, color: '#991b1b' }}>{result.error}</p>
                <button onClick={limpiar} style={{ marginTop: 8, background: '#dc2626', color: 'white', border: 'none', borderRadius: 6, padding: '6px 14px', fontWeight: 600, fontSize: 12, cursor: 'pointer' }}>Intentar de nuevo</button>
              </div>
            )}
          </div>
        </section>
      </main>
    </div>
  );
}
