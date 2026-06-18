import { useState, useEffect } from 'react';
import { toast } from '../utils/toast';
import { useNavigate } from 'react-router-dom';
import { getCatalogo, createJuguete, updateJuguete } from '../services/asistencia.api';
import type { Juguete } from '../types';
import { Package, Plus, X, ChevronLeft, Camera, Pencil } from 'lucide-react';
import api from '../services/api';

const CATEGORIAS = [
  'ENTRE 0-1', 'ENTRE 01.1-2', 'ENTRE 02.1-3', 'ENTRE 03.1-4',
  'ENTRE 04.1-5', 'ENTRE 05.1-6', 'ENTRE 06.1-7', 'ENTRE 07.1-8',
  'ENTRE 08.1-9', 'ENTRE 09.1-10', 'ENTRE 10.1-11', 'ENTRE 11.1-11.99',
];

export default function CatalogPage() {
  const navigate = useNavigate();
  const [juguetes, setJuguetes] = useState<Juguete[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<number | null>(null);
  const [form, setForm] = useState({ categoria: '', genero: '', nombreJuguete: '', stockInicial: 0 });
  const [foto, setFoto] = useState<File | null>(null);
    const [filterText, setFilterText] = useState('');
  const [pagina, setPagina] = useState(1);
  const porPag = 15;
  const [movtoJuguete, setMovtoJuguete] = useState<Juguete | null>(null);
  const [movtos, setMovtos] = useState<any[]>([]);
  const [loadingMovtos, setLoadingMovtos] = useState(false);

  const show = (msg: string, type: 'success' | 'error' = 'success') => toast(msg, type);

  const load = () => getCatalogo().then(setJuguetes).catch(() => {});

  useEffect(() => { load(); }, []);

  const openCreate = () => {
    setEditId(null);
    setForm({ categoria: '', genero: '', nombreJuguete: '', stockInicial: 0 });
    setFoto(null);
    setShowForm(true);
  };

  const openEdit = (j: Juguete) => {
    setEditId(j.id);
    setForm({ categoria: j.categoria, genero: j.genero, nombreJuguete: j.nombreJuguete, stockInicial: j.stockInicial });
    setFoto(null);
    setShowForm(true);
  };

  const handleSave = async () => {
    const fd = new FormData();
    fd.append('categoria', form.categoria);
    fd.append('genero', form.genero);
    fd.append('nombreJuguete', form.nombreJuguete);
    fd.append('stockInicial', String(form.stockInicial));
    if (foto) fd.append('foto', foto);

    try {
      if (editId) {
        await updateJuguete(editId, fd);
        show('Juguete actualizado');
      } else {
        await createJuguete(fd);
        show('Juguete creado');
      }
      setShowForm(false);
      await load();
    } catch { show('Error al guardar', 'error'); }
  };

  return (
    <div className="app-shell" style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      <header className="page-header" style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 24px' }}>
          <button onClick={() => navigate('/')} style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <ChevronLeft className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Dashboard
          </button>
          <div>
            <div style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22 }}>
              <Package className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Catálogo de Juguetes
            </div>
            <p style={{ fontSize: 13, opacity: 0.75, margin: 0, textAlign: 'center' }}>Stock aprobado para despacho</p>
          </div>
          <button onClick={openCreate} style={{ background: 'rgba(255,255,255,0.2)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <Plus className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Nuevo
          </button>
        </div>
      </header>

      <main style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
        <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', overflow: 'hidden' }}>
          <div style={{ padding: '12px 20px', borderBottom: '1px solid #e5e7eb', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
            <h2 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 16, margin: 0 }}>
              <Package className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Inventario ({juguetes.length})
            </h2>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input type="text" value={filterText} onChange={e => { setFilterText(e.target.value); setPagina(1); }}
                placeholder="🔍 Buscar..."
                style={{ padding: '6px 12px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 12, outline: 'none', width: 180 }} />
              <button onClick={() => {
                const csv = ['Nombre,Categoría,Género,Stock Inicial,Stock Actual,Foto', ...juguetes.map(j => `"${j.nombreJuguete}","${j.categoria}",${j.genero === 'F' ? 'Niñas' : j.genero === 'M' ? 'Niños' : 'Unisex'},${j.stockInicial},${j.stockActual},${j.fotoUrl ? 'Si' : 'No'}`)].join('\n');
                const blob = new Blob(['\uFEFF'+csv], {type:'text/csv;charset=utf-8;'}); const a = document.createElement('a'); a.href=URL.createObjectURL(blob); a.download='inventario.csv'; a.click();
              }} style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 6, padding: '6px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>
                📥 Excel
              </button>
            </div>
          </div>
          <div style={{ overflowX: 'auto' }}>
            {(() => {
              const filtrados = juguetes.filter(j => !filterText || j.nombreJuguete.toUpperCase().includes(filterText.toUpperCase()) || j.categoria.toUpperCase().includes(filterText.toUpperCase()));
              const paginados = filtrados.slice((pagina - 1) * porPag, pagina * porPag);
              const totalPag = Math.max(1, Math.ceil(filtrados.length / porPag));
              return (
                <>
                  <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                    <thead>
                      <tr style={{ background: '#f8fafc' }}>
                        <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Nombre</th>
                        <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Categoría</th>
                        <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Género</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Stock</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Foto</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Acciones</th>
                      </tr>
                    </thead>
                    <tbody>
                      {paginados.map((j) => (
                        <tr key={j.id} style={{ borderBottom: '1px solid #e5e7eb' }}>
                          <td style={{ padding: '12px 16px', fontWeight: 600 }}>{j.nombreJuguete}</td>
                          <td style={{ padding: '12px 16px', color: '#6b7280' }}>{j.categoria}</td>
                          <td style={{ padding: '12px 16px' }}>
                            <span style={{ background: j.genero === 'F' ? '#fce7f3' : j.genero === 'M' ? '#dbeafe' : '#f3f4f6', color: '#374151', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>
                              {j.genero === 'F' ? 'Niñas' : j.genero === 'M' ? 'Niños' : 'Unisex'}
                            </span>
                          </td>
                          <td style={{ padding: '12px 16px', textAlign: 'center' }}>
                            <span style={{ background: j.stockActual <= 5 ? '#fee2e2' : j.stockActual <= 10 ? '#fef3c7' : '#d1fae5', color: j.stockActual <= 5 ? '#991b1b' : j.stockActual <= 10 ? '#92400e' : '#065f46', padding: '2px 10px', borderRadius: 4, fontSize: 12, fontWeight: 700 }}>
                              {j.stockActual}
                            </span>
                          </td>
                          <td style={{ padding: '12px 16px', textAlign: 'center' }}>
                            {j.fotoUrl ? (
                              <div style={{ position: 'relative', display: 'inline-block' }}>
                                <img src={j.fotoUrl} alt="" style={{ width: 40, height: 40, borderRadius: 6, objectFit: 'cover', border: '1px solid #e5e7eb', cursor: 'pointer' }}
                                  onClick={() => window.open(j.fotoUrl || '', '_blank')} />
                              </div>
                            ) : <span style={{ color: '#9ca3af', fontSize: 11 }}>—</span>}
                          </td>
                          <td style={{ padding: '12px 16px', textAlign: 'center' }}>
                            <button onClick={() => openEdit(j)} style={{ background: 'none', border: '1px solid #e5e7eb', borderRadius: 6, padding: '4px 8px', cursor: 'pointer', color: '#6b7280', marginRight: 4 }}>
                              <Pencil className="w-4 h-4" />
                            </button>
                            <button onClick={async () => {
                              setMovtoJuguete(j);
                              setLoadingMovtos(true);
                              setMovtos([]);
                              try {
                                const { data } = await api.get('/dispatch/event/1/summary', { params: { pagina: 1, porPagina: 200 } });
                                const d = data.data || data;
                                const filtrados = (d.data || []).filter((m: any) => m.nombreJuguete === j.nombreJuguete);
                                setMovtos(filtrados);
                              } catch {}
                              setLoadingMovtos(false);
                            }} style={{ background: 'none', border: '1px solid #e5e7eb', borderRadius: 6, padding: '4px 8px', cursor: 'pointer', color: '#6b7280' }}>
                              👁️
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                  {filtrados.length === 0 && (
                    <div style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>
                      <Package className="w-10 h-10" style={{ opacity: 0.3, margin: '0 auto 12px' }} />
                      <p>{filterText ? 'No se encontraron juguetes con ese filtro' : 'No hay juguetes en el catálogo'}</p>
                    </div>
                  )}
                  {totalPag > 1 && (
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '10px 20px', borderTop: '1px solid #e5e7eb', fontSize: 12, color: '#6b7280' }}>
                      <span>Pág {pagina} de {totalPag} ({filtrados.length} registros)</span>
                      <div style={{ display: 'flex', gap: 6 }}>
                        <button onClick={() => setPagina(p => Math.max(1, p - 1))} disabled={pagina === 1}
                          style={{ padding: '4px 10px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagina === 1 ? 'not-allowed' : 'pointer', opacity: pagina === 1 ? 0.4 : 1 }}>← Ant.</button>
                        <button onClick={() => setPagina(p => Math.min(totalPag, p + 1))} disabled={pagina >= totalPag}
                          style={{ padding: '4px 10px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagina >= totalPag ? 'not-allowed' : 'pointer', opacity: pagina >= totalPag ? 0.4 : 1 }}>Sig. →</button>
                      </div>
                    </div>
                  )}
                </>
              );
            })()}
          </div>
        </section>
      </main>

      {/* Modal Crear/Editar */}
      {showForm && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 50, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.4)' }}
          onClick={() => setShowForm(false)}>
          <div onClick={(e) => e.stopPropagation()} style={{ background: 'white', borderRadius: 16, padding: 24, width: '90%', maxWidth: 520, margin: 20 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
              <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, margin: 0 }}>
                {editId ? '✏️ Editar Juguete' : '🎁 Nuevo Juguete'}
              </h3>
              <button onClick={() => setShowForm(false)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#9ca3af' }}>
                <X className="w-5 h-5" />
              </button>
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 6, display: 'block' }}>Nombre del Juguete</label>
                <input value={form.nombreJuguete} onChange={(e) => setForm({ ...form, nombreJuguete: e.target.value })}
                  style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14 }} />
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
                <div>
                  <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 6, display: 'block' }}>Categoría</label>
                  <select value={form.categoria} onChange={(e) => setForm({ ...form, categoria: e.target.value })}
                    style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14 }}>
                    <option value="">Seleccionar</option>
                    {CATEGORIAS.map((c) => (<option key={c} value={c}>{c}</option>))}
                  </select>
                </div>
                <div>
                  <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 6, display: 'block' }}>Género</label>
                  <select value={form.genero} onChange={(e) => setForm({ ...form, genero: e.target.value })}
                    style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14 }}>
                    <option value="">Seleccionar</option>
                    <option value="M">Niños</option>
                    <option value="F">Niñas</option>
                    <option value="TODOS">Unisex</option>
                  </select>
                </div>
                <div>
                  <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 6, display: 'block' }}>Stock</label>
                  <input type="number" value={form.stockInicial} onChange={(e) => setForm({ ...form, stockInicial: parseInt(e.target.value) || 0 })}
                    style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14 }} />
                </div>
              </div>
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 6, display: 'block' }}>
                  <Camera className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Foto
                </label>
                {/* Preview de foto existente o nueva */}
                {(foto || (editId && juguetes.find(j => j.id === editId)?.fotoUrl)) && (
                  <div style={{ marginBottom: 8 }}>
                    <img src={foto ? URL.createObjectURL(foto) : juguetes.find(j => j.id === editId)?.fotoUrl || ''} alt="Preview"
                      style={{ width: '100%', maxHeight: 160, borderRadius: 8, objectFit: 'contain', border: '1px solid #e5e7eb', background: '#f8fafc' }} />
                  </div>
                )}
                <label style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '10px 14px', borderRadius: 8, border: '1px solid #e5e7eb', background: '#f8fafc', cursor: 'pointer' }}>
                  <Camera className="w-5 h-5" style={{ color: '#9ca3af' }} />
                  <span style={{ fontSize: 13, color: '#6b7280' }}>{foto ? foto.name : 'Seleccionar imagen'}</span>
                  <input type="file" accept="image/*" style={{ display: 'none' }} onChange={(e) => setFoto(e.target.files?.[0] || null)} />
                </label>
              </div>
              <button onClick={handleSave}
                style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 8, padding: 12, fontWeight: 700, fontSize: 14, cursor: 'pointer' }}>
                {editId ? 'Guardar Cambios' : 'Crear Juguete'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal movimientos */}
      {movtoJuguete && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 50, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(0,0,0,0.4)' }}
          onClick={() => { setMovtoJuguete(null); setMovtos([]); }}>
          <div onClick={(e) => e.stopPropagation()} style={{ background: 'white', borderRadius: 16, padding: 24, width: '90%', maxWidth: 600, maxHeight: '80vh', overflow: 'auto', margin: 20 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, margin: 0 }}>
                📦 {movtoJuguete.nombreJuguete}
              </h3>
              <button onClick={() => { setMovtoJuguete(null); setMovtos([]); }} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#9ca3af' }}>
                <X className="w-5 h-5" />
              </button>
            </div>
            <div style={{ marginBottom: 12, display: 'flex', gap: 16, fontSize: 13 }}>
              <span><strong>Categoría:</strong> {movtoJuguete.categoria}</span>
              <span><strong>Stock:</strong> {movtoJuguete.stockActual}/{movtoJuguete.stockInicial}</span>
            </div>
            {movtos.length > 0 ? (
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
                  <thead>
                    <tr style={{ background: '#f8fafc' }}>
                      <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Colaborador</th>
                      <th style={{ padding: '8px 12px', textAlign: 'left', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Hijo</th>
                      <th style={{ padding: '8px 12px', textAlign: 'center', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Tipo</th>
                      <th style={{ padding: '8px 12px', textAlign: 'center', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Fecha</th>
                      <th style={{ padding: '8px 12px', textAlign: 'center', fontSize: 10, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Usuario</th>
                    </tr>
                  </thead>
                  <tbody>
                    {movtos.map((m: any, i: number) => (
                      <tr key={i} style={{ borderBottom: '1px solid #e5e7eb' }}>
                        <td style={{ padding: '8px 12px', fontWeight: 600 }}>{m.colaboradorNombre}</td>
                        <td style={{ padding: '8px 12px' }}>{m.hijoNombre}</td>
                        <td style={{ padding: '8px 12px', textAlign: 'center' }}>
                          <span style={{ background: m.estado === 'DELIVERED' ? '#d1fae5' : '#fee2e2', color: m.estado === 'DELIVERED' ? '#065f46' : '#991b1b', padding: '2px 7px', borderRadius: 4, fontSize: 10, fontWeight: 700 }}>
                            {m.estado === 'DELIVERED' ? 'Entrega' : 'Reversión'}
                          </span>
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'center', fontSize: 11, color: '#6b7280' }}>
                          {m.fechaEntrega ? new Date(m.fechaEntrega).toLocaleString() : (m.fechaReversion ? new Date(m.fechaReversion).toLocaleString() : '-')}
                        </td>
                        <td style={{ padding: '8px 12px', textAlign: 'center', fontSize: 11 }}>
                          {m.estado === 'DELIVERED' ? m.usuarioDespacho : m.usuarioReversion || '-'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div style={{ textAlign: 'center', padding: 30, color: '#9ca3af', fontSize: 13 }}>
                {loadingMovtos ? 'Cargando...' : 'No hay movimientos registrados para este juguete.'}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
