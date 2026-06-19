import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ChevronLeft, FileText, Search } from 'lucide-react';
import api from '../services/api';

interface EntregaAudit {
  entregaId: number;
  eventoId: number;
  eventoNombre: string;
  colaboradorCarnet: string;
  colaboradorNombre: string;
  hijoNombre: string;
  nombreJuguete: string;
  estado: string;
  recibidoPor: string;
  fechaEntrega: string;
  usuarioDespacho: string;
  fechaReversion: string | null;
  usuarioReversion: string | null;
  motivoReversion: string | null;
  receptorFinal?: string;
}

export default function HistoryPage() {
  const navigate = useNavigate();
  const [data, setData] = useState<EntregaAudit[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [busqueda, setBusqueda] = useState('');
  const porPagina = 25;

  const load = async (p: number, q: string) => {
    try {
      const params: any = { eventoId: 1, pagina: p, porPagina };
      if (q) params.busqueda = q;
      const res = await api.get('/dispatch/event/1/summary', { params });
      const items = res.data.data || [];
      setData(items);
      setTotal(res.data.total || 0);
    } catch { setData([]); }
  };

  useEffect(() => { load(pagina, busqueda); }, [pagina]);

  const handleSearch = () => { setPagina(1); load(1, busqueda); };
  const totalPaginas = Math.ceil(total / porPagina);

  return (
    <div style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      {/* Header */}
      <div style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white', padding: '16px 24px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <button onClick={() => navigate('/')}
            style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <ChevronLeft className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Dashboard
          </button>
          <h2 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22, margin: 0 }}>
            <FileText className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Historial de Movimientos
          </h2>
          <div />
        </div>
      </div>

      <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
        {/* Search + filters */}
        <div style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', padding: 16, marginBottom: 20 }}>
          <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
            <div style={{ flex: 1, position: 'relative' }}>
              <Search className="w-4 h-4" style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: '#9ca3af' }} />
              <input type="text" value={busqueda} onChange={(e) => setBusqueda(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                placeholder="Buscar por carnet, colaborador o hijo..."
                style={{ width: '100%', padding: '10px 14px 10px 36px', borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14, outline: 'none' }} />
            </div>
            <button onClick={handleSearch}
              style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 8, padding: '10px 20px', fontWeight: 600, fontSize: 13, cursor: 'pointer' }}>
              Buscar
            </button>
            <div style={{ fontSize: 13, color: '#6b7280', fontWeight: 600 }}>
              {total} registros
            </div>
          </div>
        </div>

        {/* Table */}
        <div style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', overflow: 'hidden' }}>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
              <thead>
                <tr style={{ background: '#da121a', color: 'white' }}>
                  <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Colaborador</th>
                  <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Hijo</th>
                  <th style={{ padding: '10px 14px', textAlign: 'left', fontWeight: 600 }}>Juguete</th>
                  <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Estado</th>
                  <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Recibido</th>
                  <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Fecha</th>
                  <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Despachó</th>
                  <th style={{ padding: '10px 14px', textAlign: 'center', fontWeight: 600 }}>Reversión</th>
                </tr>
              </thead>
              <tbody>
                {data.map((row) => (
                  <tr key={row.entregaId} style={{ borderBottom: '1px solid #e5e7eb' }}>
                    <td style={{ padding: '10px 14px' }}>
                      <div style={{ fontWeight: 600 }}>{row.colaboradorNombre}</div>
                      <div style={{ fontSize: 11, color: '#da121a' }}>{row.colaboradorCarnet}</div>
                    </td>
                    <td style={{ padding: '10px 14px', fontWeight: 600 }}>{row.hijoNombre}</td>
                    <td style={{ padding: '10px 14px', color: '#6b7280' }}>{row.nombreJuguete}</td>
                    <td style={{ padding: '10px 14px', textAlign: 'center' }}>
                      <span style={{
                        padding: '3px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700,
                        ...(row.estado === 'DELIVERED' ? { background: '#d1fae5', color: '#065f46' } : { background: '#fee2e2', color: '#991b1b' })
                      }}>
                        {row.estado === 'DELIVERED' ? 'Entregado' : 'Reversado'}
                      </span>
                    </td>
                    <td style={{ padding: '10px 14px', textAlign: 'center', fontSize: 11 }}>
                      {row.recibidoPor === 'TERCERO' ? `${row.recibidoPor}: ${row.receptorFinal || ''}` : row.recibidoPor}
                    </td>
                    <td style={{ padding: '10px 14px', textAlign: 'center', fontSize: 11, color: '#6b7280' }}>
                      {row.fechaEntrega ? new Date(row.fechaEntrega).toLocaleString() : '-'}
                    </td>
                    <td style={{ padding: '10px 14px', textAlign: 'center', fontSize: 11 }}>{row.usuarioDespacho}</td>
                    <td style={{ padding: '10px 14px', textAlign: 'center', fontSize: 11 }}>
                      {row.estado === 'REVERTED' ? (
                        <div>
                          <span style={{ color: '#dc2626' }}>↺ {row.usuarioReversion}</span>
                          <div style={{ color: '#9ca3af', fontSize: 10 }}>{row.motivoReversion?.slice(0, 40)}</div>
                        </div>
                      ) : '-'}
                    </td>
                  </tr>
                ))}
                {data.length === 0 && (
                  <tr><td colSpan={8} style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>No hay movimientos registrados</td></tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPaginas > 1 && (
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '12px 20px', borderTop: '1px solid #e5e7eb' }}>
              <span style={{ fontSize: 12, color: '#6b7280' }}>Página {pagina} de {totalPaginas}</span>
              <div style={{ display: 'flex', gap: 8 }}>
                <button onClick={() => setPagina(p => Math.max(1, p - 1))} disabled={pagina === 1}
                  style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, fontWeight: 600, cursor: pagina === 1 ? 'not-allowed' : 'pointer', opacity: pagina === 1 ? 0.5 : 1 }}>
                  Anterior
                </button>
                <button onClick={() => setPagina(p => Math.min(totalPaginas, p + 1))} disabled={pagina >= totalPaginas}
                  style={{ padding: '6px 14px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, fontWeight: 600, cursor: pagina >= totalPaginas ? 'not-allowed' : 'pointer', opacity: pagina >= totalPaginas ? 0.5 : 1 }}>
                  Siguiente
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
