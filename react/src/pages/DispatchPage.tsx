import { useState, useEffect, useRef } from 'react';
import { toast } from '../utils/toast';
import { useNavigate } from 'react-router-dom';
import { getCenso, getColaboradorFull, registrarAsistencia, registrarEntrega, reversarEntrega, updateFotoEvidencia } from "./../services/asistencia.api";
import type { ColaboradorFicha, Hijo, CensoItem, Juguete } from '../types';
import { getCatalogo } from '../services/asistencia.api';
import Swal from 'sweetalert2';
import { Gift, ChevronLeft, Camera, Check, RotateCcw, X, Loader2, Users, ChevronRight, Package, Heart, AlertTriangle } from 'lucide-react';

const EVENTO_ACTIVO_ID = 1;

export default function DispatchPage() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [ficha, setFicha] = useState<ColaboradorFicha | null>(null);
    const [showDeliver, setShowDeliver] = useState<{ hijo: Hijo; fotoEvidencia?: File | null } | null>(null);
  const [showRevert, setShowRevert] = useState<{ hijo: Hijo; entregaId: number } | null>(null);
  const [colaboradorFoto, setColaboradorFoto] = useState<File | null>(null);
  const [recibidoPor, setRecibidoPor] = useState('COLABORADOR');
  const [nombreReceptor, setNombreReceptor] = useState('');
  const [juguetesDisponibles, setJuguetesDisponibles] = useState<Juguete[]>([]);
  const [selectedJuguetes, setSelectedJuguetes] = useState<Record<number, number>>({});
  const [fotoPreview, setFotoPreview] = useState<string | null>(null);
  const [jugueteIdDeliver, setJugueteIdDeliver] = useState(0);
  const [deliverFoto, setDeliverFoto] = useState<File | null>(null);
  const [showCam, setShowCam] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);

  const startCam = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } } });
      streamRef.current = stream;
      if (videoRef.current) videoRef.current.srcObject = stream;
    } catch { alert('No se pudo acceder a la cámara'); setShowCam(false); }
  };

  const stopCam = () => {
    if (streamRef.current) { streamRef.current.getTracks().forEach(t => t.stop()); streamRef.current = null; }
  };

  const capturarFoto = () => {
    const video = videoRef.current;
    if (!video) return;
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')?.drawImage(video, 0, 0);
    canvas.toBlob((blob) => {
      if (blob) {
        const file = new File([blob], 'camara.jpg', { type: 'image/jpeg' });
        if (showDeliver) setDeliverFoto(file);
        else setColaboradorFoto(file);
      }
    }, 'image/jpeg', 0.8);
    stopCam();
  };
  const [asistidos, setAsistidos] = useState<CensoItem[]>([]);
  const [loadingAsistidos, setLoadingAsistidos] = useState(true);
  const [filterPendientes, setFilterPendientes] = useState('');
  const [pagPendientes, setPagPendientes] = useState(1);
  const [filterCompletos, setFilterCompletos] = useState('');
  const [pagCompletos, setPagCompletos] = useState(1);
  const porPagPendientes = 3;
  const porPagCompletos = 5;

  // Load catalog for alternative juguetes
  useEffect(() => { getCatalogo().then(setJuguetesDisponibles).catch(() => {}); }, []);

  // Load employees who have attended
  useEffect(() => {
    setLoadingAsistidos(true);
    (async () => {
      const first = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, 1, 200).catch(() => null);
      if (!first) return;
      let allData = first.data || [];
      for (let p = 2; p <= (first.totalPaginas || 1); p++) {
        const res = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, p, 200).catch(() => null);
        if (res) allData = allData.concat(res.data || []);
      }
      setAsistidos(allData.filter((c: CensoItem) => c.Asistio > 0));
    })();
    setLoadingAsistidos(false);
  }, []);

  const refreshAsistidos = async () => {
    const first = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, 1, 200).catch(() => null);
    if (!first) return;
    let allData = first.data || [];
    for (let p = 2; p <= (first.totalPaginas || 1); p++) {
      const res = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, p, 200).catch(() => null);
      if (res) allData = allData.concat(res.data || []);
    }
    setAsistidos(allData.filter((c: CensoItem) => c.Asistio > 0));
  };

  const recargarCatalogo = () => getCatalogo().then(setJuguetesDisponibles).catch(() => {});
  const show = (msg: string, type: 'success' | 'error' = 'success') => toast(msg, type);

  const handleRegistrarAsistencia = async (carnet: string) => {
    try {
      await registrarAsistencia(EVENTO_ACTIVO_ID, carnet);
      show('Asistencia registrada');
      // Refresh
      const data = await getColaboradorFull(carnet, EVENTO_ACTIVO_ID);
      setFicha(data);
      const res = await getCenso(EVENTO_ACTIVO_ID, undefined, undefined, 1, 200);
      setAsistidos(res.data.filter((c: CensoItem) => c.Asistio > 0));
    } catch { show('Error al registrar asistencia', 'error'); }
  };

  const handleEntrega = async (hijoId: number, jugueteId: number, recibidoPor: string, nombreReceptor: string | null, carnetColab: string, foto?: File) => {
    const fd = new FormData();
    fd.append('eventoId', String(EVENTO_ACTIVO_ID));
    fd.append('hijoId', String(hijoId));
    fd.append('jugueteId', String(jugueteId));
    fd.append('carnetColaborador', carnetColab);
    fd.append('recibidoPor', recibidoPor);
    if (nombreReceptor) fd.append('nombreReceptor', nombreReceptor);
    const fotoSubir = foto || colaboradorFoto;
    if (fotoSubir) fd.append('foto', fotoSubir);
    try {
      await registrarEntrega(fd);
      show(`🎁 Juguete entregado`);
      setShowDeliver(null);
      await recargarCatalogo();
      const data = await getColaboradorFull(carnetColab, EVENTO_ACTIVO_ID);
      setFicha(data);
      refreshAsistidos();
    } catch { show('Error al entregar', 'error'); }
  };

  const handleRevert = async (entregaId: number, motivo: string) => {
    try {
      await reversarEntrega(entregaId, motivo);
      show('↺ Entrega reversada');
      setShowRevert(null);
      await recargarCatalogo();
      if (ficha) {
        const data = await getColaboradorFull(ficha.colaborador.carnet, EVENTO_ACTIVO_ID);
        setFicha(data);
      }
      refreshAsistidos();
    } catch { show('Error al reversar', 'error'); }
  };

  const abrirColaborador = async (carnet: string) => {
    setLoading(true);
    setFicha(null);
    setColaboradorFoto(null);
    try {
      const data = await getColaboradorFull(carnet, EVENTO_ACTIVO_ID);
      setFicha(data);
    } catch { show('Error al cargar', 'error'); }
    finally { setTimeout(() => setLoading(false), 300); }
  };

  const fotoUrlExistente = ficha?.hijos.find(h => h.fotoEvidenciaUrl)?.fotoEvidenciaUrl;

  const abrirEvidencia = async (carnet: string) => {
    try {
      const data = await getColaboradorFull(carnet, EVENTO_ACTIVO_ID);
      const hijosConFoto = data.hijos.filter((h: any) => h.fotoEvidenciaUrl);
      if (hijosConFoto.length > 0) {
        setFotoPreview(hijosConFoto[0].fotoEvidenciaUrl);
      } else {
        Swal.fire({
          icon: 'info',
          title: 'Sin foto de evidencia',
          text: 'Este colaborador no tiene fotos de evidencia registradas.',
          confirmButtonText: 'Entendido',
          confirmButtonColor: '#da121a',
        });
      }
    } catch {
      Swal.fire({
        icon: 'error',
        title: 'Error',
        text: 'No se pudo cargar la información.',
        confirmButtonText: 'Cerrar',
      });
    }
  };

  return (
    <div className="app-shell" style={{ background: '#f8f9fa', minHeight: '100vh' }}>
      {/* Header */}
      <header className="page-header" style={{ background: 'linear-gradient(135deg, #da121a 0%, #1e1e1e 100%)', color: 'white' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8, padding: '16px 24px' }}>
          <button onClick={() => navigate('/')} style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <ChevronLeft className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Dashboard
          </button>
          <h2 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, fontSize: 22, margin: 0 }}>
            <Gift className="w-5 h-5" style={{ verticalAlign: 'middle', marginRight: 8 }} /> Despacho de Juguetes
          </h2>
          <button onClick={async () => {
            setFicha(null);
            setColaboradorFoto(null);
            await Promise.all([recargarCatalogo(), refreshAsistidos()]);
            show('🔄 Datos actualizados');
          }} style={{ background: 'rgba(255,255,255,0.15)', border: 'none', color: 'white', borderRadius: '20px', padding: '6px 16px', fontWeight: 700, fontSize: 13, cursor: 'pointer' }}>
            <RotateCcw className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Refrescar
          </button>
        </div>
        <p style={{ fontSize: 13, opacity: 0.75, textAlign: 'center', margin: '0 24px 8px' }}>Control de Entregas e Inventario</p>
        {/* Mini stats - basados en asistidos */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12, padding: '0 24px 12px' }}>
          {(() => {
            const totalHijos = asistidos.reduce((s, r) => s + r.TotalHijos, 0);
            const entregados = asistidos.reduce((s, r) => s + r.Entregados, 0);
            return [
              { label: 'Hijos a Despachar', value: totalHijos, gradient: 'linear-gradient(135deg, #374151 0%, #111827 100%)', icon: Package },
              { label: 'Entregados', value: entregados, gradient: 'linear-gradient(135deg, #10b981 0%, #059669 100%)', icon: Heart },
              { label: 'Por Despachar', value: totalHijos - entregados, gradient: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)', icon: AlertTriangle },
            ];
          })().map((s) => (
            <div key={s.label} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 14px', borderRadius: 8, background: s.gradient, color: 'white' }}>
              <div style={{ width: 32, height: 32, borderRadius: 8, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'rgba(255,255,255,0.2)' }}>
                <s.icon className="w-5 h-5" />
              </div>
              <div>
                <div style={{ fontSize: 11, fontWeight: 700, textTransform: 'uppercase', opacity: 0.85 }}>{s.label}</div>
                <div style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 800, fontSize: 20 }}>{s.value}</div>
              </div>
            </div>
          ))}
        </div>
      </header>

      <main style={{ display: 'grid', gridTemplateColumns: '320px 1fr', gap: 20, padding: 24, maxWidth: 1400, margin: '0 auto' }}>

        {/* LEFT: Lista de colaboradores que asistieron */}
        <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', height: 'fit-content' }}>
            <div style={{ background: '#da121a', color: 'white', padding: '10px 16px', borderRadius: '12px 12px 0 0', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span style={{ fontWeight: 700, fontSize: 14 }}>
              <Users className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Pendientes ({asistidos.filter(a => a.Entregados < a.TotalHijos).length})
            </span>
            <button onClick={async () => { await Promise.all([recargarCatalogo(), refreshAsistidos()]); setFicha(null); setColaboradorFoto(null); }}
              style={{ background: 'rgba(255,255,255,0.2)', border: 'none', color: 'white', borderRadius: 6, padding: '4px 10px', fontWeight: 600, fontSize: 11, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 4 }}>
              <RotateCcw className="w-3 h-3" /> Refrescar
            </button>
          </div>
            <div style={{ padding: 12 }}>
              <input type="text" value={filterPendientes} onChange={e => { setFilterPendientes(e.target.value); setPagPendientes(1); }}
                placeholder="🔍 Buscar por nombre..."
                style={{ width: '100%', padding: '8px 12px', borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 12, outline: 'none', marginBottom: 8, boxSizing: 'border-box' }} />

              {loadingAsistidos ? (
                <div style={{ textAlign: 'center', padding: 20, color: '#9ca3af' }}>
                  <Loader2 className="w-5 h-5" style={{ margin: '0 auto 8px', animation: 'spin 0.8s linear infinite' }} />
                  <span style={{ fontSize: 12 }}>Cargando...</span>
                </div>
              ) : asistidos.length === 0 ? (
                <div style={{ textAlign: 'center', padding: 20, color: '#9ca3af', fontSize: 12 }}>
                  No hay colaboradores que hayan asistido al evento.
                </div>
              ) : (() => {
                const filtrados = asistidos.filter(a => a.Entregados < a.TotalHijos && (!filterPendientes || a.Nombre.toUpperCase().includes(filterPendientes.toUpperCase())));
                const paginados = filtrados.slice((pagPendientes - 1) * porPagPendientes, pagPendientes * porPagPendientes);
                const totalPag = Math.max(1, Math.ceil(filtrados.length / porPagPendientes));
                return (
                  <>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 4, maxHeight: 380, overflowY: 'auto' }}>
                      {paginados.map((row) => (
                        <button key={row.Carnet} onClick={() => { abrirColaborador(row.Carnet); setPagPendientes(1); }}
                          style={{
                            display: 'flex', alignItems: 'center', gap: 10, padding: '8px 12px',
                            borderRadius: 8, border: 'none', background: ficha?.colaborador.carnet === row.Carnet ? '#fef2f2' : 'transparent',
                            cursor: 'pointer', textAlign: 'left', width: '100%', fontSize: 13, transition: '0.15s'
                          }}>
                          <div style={{ width: 32, height: 32, borderRadius: '50%', background: '#da121a', color: 'white', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: 12, flexShrink: 0 }}>
                            {row.Nombre.charAt(0)}
                          </div>
                          <div style={{ flex: 1, minWidth: 0 }}>
                            <div style={{ fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{row.Nombre}</div>
                            <div style={{ fontSize: 11, color: '#6b7280' }}>{row.Carnet} · {row.Entregados}/{row.TotalHijos} entregados</div>
                          </div>
                          <ChevronRight className="w-4 h-4" style={{ color: '#9ca3af', flexShrink: 0 }} />
                        </button>
                      ))}
                    </div>
                    {totalPag > 1 && (
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 8, fontSize: 11, color: '#6b7280' }}>
                        <span>Pág {pagPendientes} de {totalPag}</span>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button onClick={() => setPagPendientes(p => Math.max(1, p - 1))} disabled={pagPendientes === 1}
                            style={{ padding: '2px 8px', borderRadius: 4, border: '1px solid #e5e7eb', background: 'white', fontSize: 11, cursor: pagPendientes === 1 ? 'not-allowed' : 'pointer', opacity: pagPendientes === 1 ? 0.4 : 1 }}>←</button>
                          <button onClick={() => setPagPendientes(p => Math.min(totalPag, p + 1))} disabled={pagPendientes >= totalPag}
                            style={{ padding: '2px 8px', borderRadius: 4, border: '1px solid #e5e7eb', background: 'white', fontSize: 11, cursor: pagPendientes >= totalPag ? 'not-allowed' : 'pointer', opacity: pagPendientes >= totalPag ? 0.4 : 1 }}>→</button>
                        </div>
                      </div>
                    )}
                  </>
                );
              })()}
            </div>
        </section>

        {/* RIGHT: Ficha del colaborador seleccionado */}
        <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)' }}>
          <div style={{ padding: 20 }}>
            {loading ? (
              <div style={{ textAlign: 'center', padding: 60, color: '#9ca3af' }}>
                <Loader2 className="w-8 h-8" style={{ margin: '0 auto 12px', animation: 'spin 0.8s linear infinite' }} />
                <p style={{ fontSize: 13 }}>Cargando colaborador...</p>
              </div>
            ) : ficha ? (
              <>
                {/* Header colaborador */}
                <div style={{ display: 'flex', gap: 16, marginBottom: 20 }}>
                  <div style={{ position: 'relative' }}>
                    {ficha.fotoHcm && <img src={ficha.fotoHcm} alt="Foto" style={{ width: 80, height: 80, borderRadius: '50%', objectFit: 'cover', boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}
                      onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }} />}
                    {!ficha.fotoHcm && (
                      <div style={{ width: 80, height: 80, borderRadius: '50%', border: '3px solid #e5e7eb', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#f3f4f6', fontSize: 28, fontWeight: 700, color: '#da121a' }}>
                        {ficha.colaborador.nombre.charAt(0)}
                      </div>
                    )}
                  </div>
                  <div style={{ flex: 1 }}>
                    <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, margin: '0 0 2px' }}>{ficha.colaborador.nombre}</h3>
                    <span style={{ background: '#da121a', color: 'white', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>Carnet: {ficha.colaborador.carnet}</span>
                    <p style={{ fontSize: 12, color: '#6b7280', margin: '4px 0' }}>{ficha.colaborador.gerencia} · {ficha.colaborador.ubicacion}</p>
                    {!ficha.asistio && (
                      <button onClick={() => handleRegistrarAsistencia(ficha.colaborador.carnet)}
                        style={{ marginTop: 8, background: '#10b981', color: 'white', border: 'none', borderRadius: 8, padding: '8px 16px', fontWeight: 700, fontSize: 12, cursor: 'pointer' }}>
                        <Check className="w-3 h-3" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Registrar Asistencia
                      </button>
                    )}
                    {ficha.asistio && (
                      <span style={{ display: 'inline-block', marginTop: 8, padding: '4px 10px', background: '#d1fae5', color: '#065f46', borderRadius: 6, fontWeight: 700, fontSize: 11 }}>
                        ✅ Asistió
                      </span>
                    )}
                  </div>
                </div>

                {/* Foto evidencia - 1 por colaborador (REQUERIDO) */}
                {ficha.asistio && (
                  <div style={{ marginBottom: 16, padding: 12, background: '#f8fafc', borderRadius: 8, border: colaboradorFoto ? '2px solid #10b981' : '2px solid #ef4444' }}>
                    <p style={{ fontSize: 12, fontWeight: 700, color: '#374151', margin: '0 0 8px' }}>
                      📸 Foto de Evidencia <span style={{ color: '#ef4444', fontSize: 10 }}>(Requerido)</span>
                    </p>
                    {fotoUrlExistente ? (
                      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                        <img src={fotoUrlExistente} alt="Evidencia" style={{ width: 80, height: 80, borderRadius: 8, objectFit: 'cover', border: '1px solid #e5e7eb', cursor: 'pointer' }}
                          onClick={() => setFotoPreview(fotoUrlExistente)} />
                        <span style={{ fontSize: 12, color: '#10b981', fontWeight: 600 }}>✅ Foto ya registrada</span>
                      </div>
                    ) : (
                      <div>
                        <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
                          <label style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', padding: '8px 12px', borderRadius: 8, border: '1px dashed #d1d5db' }}>
                            <Camera className="w-5 h-5" style={{ color: '#9ca3af' }} />
                            <span style={{ fontSize: 13, color: '#6b7280' }}>{colaboradorFoto ? colaboradorFoto.name : 'Subir foto'}</span>
                            <input type="file" accept="image/*" style={{ display: 'none' }} onChange={(e) => setColaboradorFoto(e.target.files?.[0] || null)} />
                          </label>
                          <button onClick={() => { setShowCam(true); setTimeout(startCam, 100); }}
                            style={{ padding: '8px 14px', borderRadius: 8, border: '1px dashed #d1d5db', background: 'white', cursor: 'pointer', fontSize: 12, fontWeight: 600, color: '#374151' }}>
                            📷 Cámara
                          </button>
                        </div>
                        {colaboradorFoto && (
                          <img src={URL.createObjectURL(colaboradorFoto)} alt="Preview" style={{ width: '100%', maxHeight: 150, borderRadius: 8, objectFit: 'cover' }} />
                        )}
                        {/* Camera modal */}
                        {showCam && (
                          <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(0,0,0,0.9)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
                            onClick={() => { setShowCam(false); stopCam(); }}>
                            <div onClick={e => e.stopPropagation()} style={{ padding: 20 }}>
                              <video ref={videoRef} autoPlay playsInline style={{ width: '100%', maxWidth: 500, borderRadius: 12, background: '#000' }} />
                              <div style={{ display: 'flex', gap: 12, marginTop: 12, justifyContent: 'center' }}>
                                <button onClick={() => { capturarFoto(); setShowCam(false); }}
                                  style={{ background: '#10b981', color: 'white', border: 'none', borderRadius: 8, padding: '10px 24px', fontWeight: 700, fontSize: 14, cursor: 'pointer' }}>
                                  📸 Tomar Foto
                                </button>
                                <button onClick={() => { setShowCam(false); stopCam(); }}
                                  style={{ background: '#6b7280', color: 'white', border: 'none', borderRadius: 8, padding: '10px 24px', fontWeight: 600, fontSize: 14, cursor: 'pointer' }}>
                                  Cancelar
                                </button>
                              </div>
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                )}

                {/* Lista de hijos */}
                <h4 style={{ fontSize: 14, fontWeight: 700, margin: '0 0 12px', color: '#374151' }}>
                  Hijos ({ficha.hijos.filter(h => h.estadoEntrega === 'DELIVERED').length} de {ficha.hijos.length} entregados)
                </h4>

                {/* Botón Despachar Todos */}
                {ficha.asistio && ficha.hijos.some(h => h.estadoEntrega !== 'DELIVERED') && ficha.hijos.length > 1 && (
                  <div style={{ marginBottom: 12, padding: 14, background: '#fefce8', borderRadius: 10, border: '1px solid #fde68a' }}>
                    <p style={{ fontSize: 12, fontWeight: 700, color: '#92400e', margin: '0 0 8px' }}>⚡ Despachar todos los hijos de una sola vez</p>
                    <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
                      {['COLABORADOR', 'CONYUGE', 'TERCERO'].map((r) => (
                        <button key={r} onClick={() => { setRecibidoPor(r); setNombreReceptor(''); }}
                          style={{ padding: '5px 10px', borderRadius: 6, border: 'none', fontWeight: 600, fontSize: 11, cursor: 'pointer',
                            ...(recibidoPor === r ? { background: '#da121a', color: 'white' } : { background: '#f3f4f6', color: '#6b7280' }) }}>
                          {r === 'COLABORADOR' ? 'Colaborador' : r === 'CONYUGE' ? 'Cónyuge' : 'Tercero'}
                        </button>
                      ))}
                    </div>
                    {recibidoPor === 'TERCERO' && (
                      <input type="text" value={nombreReceptor} onChange={(e) => setNombreReceptor(e.target.value)}
                        placeholder="Nombre de quien recibe" style={{ width: '100%', padding: '6px 10px', borderRadius: 6, border: '1px solid #e5e7eb', fontSize: 12, marginBottom: 8, boxSizing: 'border-box' }} />
                    )}
                    <button onClick={async () => {
                      if (!colaboradorFoto) {
                        Swal.fire({
                          icon: 'error',
                          title: 'Foto requerida',
                          text: 'Debe tomar o subir una foto de evidencia antes de entregar.',
                          confirmButtonText: 'Entendido',
                          confirmButtonColor: '#da121a',
                        });
                        return;
                      }
                      const pendientes = ficha.hijos.filter(h => h.estadoEntrega !== 'DELIVERED');
                      const foto = colaboradorFoto || undefined;
                      for (const hijo of pendientes) {
                        const jugueteId = selectedJuguetes[hijo.id] || hijo.jugueteSugerido?.id || 0;
                        const fd = new FormData();
                        fd.append('eventoId', String(EVENTO_ACTIVO_ID));
                        fd.append('hijoId', String(hijo.id));
                        fd.append('jugueteId', String(jugueteId));
                        fd.append('carnetColaborador', ficha.colaborador.carnet);
                        fd.append('recibidoPor', recibidoPor);
                        if (recibidoPor === 'TERCERO' && nombreReceptor) fd.append('nombreReceptor', nombreReceptor);
                        if (foto) fd.append('foto', foto);
                        await registrarEntrega(fd);
                      }
                      await recargarCatalogo();
                      const data = await getColaboradorFull(ficha.colaborador.carnet, EVENTO_ACTIVO_ID);
                      setFicha(data);
                      refreshAsistidos();
                      show(`✅ ${pendientes.length} juguetes entregados`);
                    }}
                      style={{ width: '100%', padding: '8px 14px', background: '#da121a', color: 'white', border: 'none', borderRadius: 6, fontWeight: 700, fontSize: 12, cursor: 'pointer' }}>
                      🚀 Despachar Todo ({ficha.hijos.filter(h => h.estadoEntrega !== 'DELIVERED').length} hijos)
                    </button>
                  </div>
                )}

                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {ficha.hijos.map((hijo) => (
                    <div key={hijo.id} style={{
                      padding: 14, borderRadius: 10,
                      borderLeft: '5px solid',
                      ...(hijo.estadoEntrega === 'DELIVERED'
                        ? { background: '#ecfdf5', borderLeftColor: '#10b981' }
                        : hijo.estadoEntrega === 'REVERTED'
                        ? { background: '#fef3c7', borderLeftColor: '#f59e0b' }
                        : { background: '#f9fafb', borderLeftColor: '#d1d5db' })
                    }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <div>
                          <h5 style={{ fontWeight: 700, margin: 0, fontSize: 14 }}>
                            {hijo.generoHijo === 'F' ? '👧' : '👦'} {hijo.nombreHijo}
                          </h5>
                          <p style={{ fontSize: 12, color: '#6b7280', margin: '2px 0' }}>
                            {hijo.categoria} · {hijo.edadHijo} años
                          </p>
                          {hijo.jugueteSugerido && (() => {
                            const opciones = juguetesDisponibles.filter(j => j.categoria === hijo.categoria && (j.genero === hijo.generoHijo || j.genero === 'TODOS') && j.stockActual > 0);
                            const selId = selectedJuguetes[hijo.id] || hijo.jugueteSugerido?.id || 0;
                            const sel = juguetesDisponibles.find(j => j.id === selId) || hijo.jugueteSugerido;
                            return (
                              <div style={{ marginTop: 4 }}>
                                {opciones.length > 1 ? (
                                  <select value={selId} onChange={e => setSelectedJuguetes(prev => ({...prev, [hijo.id]: parseInt(e.target.value)}))}
                                    style={{ fontSize: 11, padding: '3px 6px', borderRadius: 4, border: '1px solid #e5e7eb', width: '100%', maxWidth: 200 }}>
                                    {opciones.map(j => (
                                      <option key={j.id} value={j.id}>{j.nombreJuguete} (Stock: {j.stockActual})</option>
                                    ))}
                                  </select>
                                ) : (
                                  <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 2 }}>
                                    {sel?.fotoUrl && (
                                      <img src={sel.fotoUrl} alt="" style={{ width: 36, height: 36, borderRadius: 6, objectFit: 'cover', border: '1px solid #e5e7eb', cursor: 'pointer' }}
                                        onClick={() => setFotoPreview(sel.fotoUrl)} />
                                    )}
                                    <span style={{ fontSize: 11, fontWeight: 600, color: sel?.stockActual ? '#10b981' : '#ef4444' }}>
                                      {sel?.nombreJuguete} (Stock: {sel?.stockActual})
                                    </span>
                                  </div>
                                )}
                              </div>
                            );
                          })()}
                        </div>
                        <div style={{ display: 'flex', gap: 8, flexShrink: 0 }}>
                          {hijo.estadoEntrega === 'DELIVERED' ? (
                            <>
                              <span style={{ background: '#10b981', color: 'white', padding: '4px 10px', borderRadius: 6, fontWeight: 700, fontSize: 11 }}>Entregado</span>
                              {hijo.fotoEvidenciaUrl && (
                                <button onClick={() => setFotoPreview(hijo.fotoEvidenciaUrl)}
                                  style={{ background: 'none', border: '1px solid #10b981', borderRadius: 6, padding: '4px 8px', cursor: 'pointer', color: '#10b981', fontSize: 14 }}>
                                  📷
                                </button>
                              )}
                              <button onClick={async () => {
                                const input = document.createElement('input');
                                input.type = 'file';
                                input.accept = 'image/*';
                                input.onchange = async () => {
                                  const file = input.files?.[0];
                                  if (!file) return;
                                  try {
                                    await updateFotoEvidencia(hijo.id, EVENTO_ACTIVO_ID, file);
                                    show('✅ Foto de evidencia actualizada');
                                    await recargarCatalogo();
                                    const data = await getColaboradorFull(ficha!.colaborador.carnet, EVENTO_ACTIVO_ID);
                                    setFicha(data);
                                    refreshAsistidos();
                                  } catch { show('Error al actualizar foto', 'error'); }
                                };
                                input.click();
                              }}
                                style={{ background: '#e0e7ff', color: '#4338ca', border: 'none', borderRadius: 6, padding: '4px 8px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>
                                <Camera className="w-3 h-3" style={{ verticalAlign: 'middle', marginRight: 2 }} /> Foto
                              </button>
                              <button onClick={() => setShowRevert({ hijo, entregaId: hijo.entregaId! })}
                                style={{ background: '#fee2e2', color: '#dc2626', border: 'none', borderRadius: 6, padding: '4px 10px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>
                                <RotateCcw className="w-3 h-3" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Reversar
                              </button>
                            </>
                           ) : (
                              <button onClick={() => { setShowDeliver({ hijo, fotoEvidencia: colaboradorFoto || null }); setRecibidoPor('COLABORADOR'); setNombreReceptor(''); setJugueteIdDeliver(selectedJuguetes[hijo.id] || hijo.jugueteSugerido?.id || 0); setDeliverFoto(null); }}
                                style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 6, padding: '6px 14px', fontWeight: 700, fontSize: 12, cursor: 'pointer' }}>
                                <Gift className="w-3 h-3" style={{ verticalAlign: 'middle', marginRight: 4 }} /> Entregar
                              </button>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div style={{ textAlign: 'center', padding: 60, color: '#9ca3af' }}>
                <Gift className="w-12 h-12" style={{ opacity: 0.3, margin: '0 auto 12px' }} />
                <p>Seleccione un colaborador de la lista o busque por carnet</p>
                <p style={{ fontSize: 12, marginTop: 4 }}>Solo colaboradores que asistieron al evento pueden recibir despacho</p>
              </div>
            )}
          </div>
        </section>

        {/* Tablas de estado de despacho */}
        {asistidos.length > 0 && (
          <div style={{ gridColumn: '1 / -1' }}>
            {/* Completos (todos los regalos entregados) */}
            <section style={{ background: 'white', borderRadius: 12, boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)', marginTop: 16 }}>
              <div style={{ background: '#10b981', color: 'white', padding: '10px 16px', borderRadius: '12px 12px 0 0', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
                <span style={{ fontWeight: 700, fontSize: 14 }}>✅ Despachados Completos</span>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                  <input type="text" value={filterCompletos} onChange={e => { setFilterCompletos(e.target.value); setPagCompletos(1); }}
                    placeholder="Filtrar por carnet o nombre..."
                    style={{ padding: '5px 10px', borderRadius: 6, border: '1px solid #d1d5db', fontSize: 12, outline: 'none', background: 'white', color: '#374151', width: 180 }} />
                  <button onClick={() => {
                    const data = asistidos.filter(a => a.Entregados === a.TotalHijos && a.TotalHijos > 0);
                    if (data.length === 0) return;
                    const csv = ['Carnet,Nombre,Hijos,Entregados', ...data.map(r => `"${r.Carnet}","${r.Nombre}",${r.TotalHijos},${r.Entregados}/${r.TotalHijos}`)].join('\n');
                    const blob = new Blob(['\uFEFF'+csv], {type:'text/csv;charset=utf-8;'}); const a = document.createElement('a'); a.href=URL.createObjectURL(blob); a.download='despachados.csv'; a.click();
                  }} style={{ background: '#10b981', border: 'none', color: 'white', borderRadius: 6, padding: '5px 12px', fontWeight: 600, fontSize: 11, cursor: 'pointer' }}>📥 Excel</button>
                </div>
              </div>
              <div style={{ overflowX: 'auto' }}>
                {(() => {
                  const completos = asistidos.filter(a => a.Entregados === a.TotalHijos && a.TotalHijos > 0 && (!filterCompletos || (a.Carnet+' '+a.Nombre).toUpperCase().includes(filterCompletos.toUpperCase())));
                  const pag = completos.slice((pagCompletos - 1) * porPagCompletos, pagCompletos * porPagCompletos);
                  return (
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                      <thead><tr style={{ background: '#f8fafc' }}>
                        <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Carnet</th>
                        <th style={{ padding: '10px 16px', textAlign: 'left', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Colaborador</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Hijos</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Entreg.</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Foto</th>
                        <th style={{ padding: '10px 16px', textAlign: 'center', fontSize: 11, fontWeight: 700, color: '#6b7280', textTransform: 'uppercase' }}>Detalle</th>
                      </tr></thead>
                      <tbody>
                        {pag.map(row => (
                          <tr key={row.Carnet} style={{ borderBottom: '1px solid #e5e7eb' }}>
                            <td style={{ padding: '10px 16px', fontWeight: 700, color: '#da121a', fontSize: 12 }}>{row.Carnet}</td>
                            <td style={{ padding: '10px 16px', fontWeight: 600 }}>{row.Nombre}</td>
                            <td style={{ padding: '10px 16px', textAlign: 'center' }}>{row.TotalHijos}</td>
                            <td style={{ padding: '10px 16px', textAlign: 'center' }}><span style={{ fontWeight: 700, color: '#10b981' }}>{row.Entregados}/{row.TotalHijos}</span></td>
                            <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                              <button onClick={() => abrirEvidencia(row.Carnet)} style={{ background: 'none', border: '1px solid #e5e7eb', borderRadius: 6, padding: '4px 8px', cursor: 'pointer', fontSize: 14 }} title="Ver fotos de evidencia">
                                📷
                              </button>
                            </td>
                            <td style={{ padding: '10px 16px', textAlign: 'center' }}>
                              <button onClick={() => abrirColaborador(row.Carnet)} style={{ background: 'none', border: '1px solid #e5e7eb', borderRadius: 6, padding: '4px 8px', cursor: 'pointer', color: '#6b7280' }}>👁️</button>
                            </td>
                          </tr>
                        ))}
                        {completos.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center', padding: 30, color: '#9ca3af' }}>No hay despachos completados aún</td></tr>}
                      </tbody>
                    </table>
                  );
                })()}
              </div>
              {(() => {
                const totalC = Math.max(1, Math.ceil(asistidos.filter(a => a.Entregados === a.TotalHijos && a.TotalHijos > 0 && (!filterCompletos || a.Nombre.toUpperCase().includes(filterCompletos.toUpperCase()))).length / porPagCompletos));
                return (
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 16px', borderTop: '1px solid #e5e7eb', fontSize: 12, color: '#6b7280' }}>
                    <span>Pág {pagCompletos} de {totalC}</span>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button onClick={() => setPagCompletos(p => Math.max(1, p - 1))} disabled={pagCompletos === 1}
                        style={{ padding: '4px 10px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagCompletos === 1 ? 'not-allowed' : 'pointer', opacity: pagCompletos === 1 ? 0.4 : 1 }}>← Ant.</button>
                      <button onClick={() => setPagCompletos(p => Math.min(totalC, p + 1))} disabled={pagCompletos >= totalC}
                        style={{ padding: '4px 10px', borderRadius: 6, border: '1px solid #e5e7eb', background: 'white', fontSize: 12, cursor: pagCompletos >= totalC ? 'not-allowed' : 'pointer', opacity: pagCompletos >= totalC ? 0.4 : 1 }}>Sig. →</button>
                    </div>
                  </div>
                );
              })()}
            </section>
          </div>
        )}
      </main>

      {/* Deliver Modal */}
      {showDeliver && ficha && (
        <div style={{
          position: 'fixed', inset: 0, zIndex: 50, display: 'flex', alignItems: 'center', justifyContent: 'center',
          background: 'rgba(0,0,0,0.4)'
        }} onClick={() => setShowDeliver(null)}>
          <div onClick={(e) => e.stopPropagation()} style={{ background: 'white', borderRadius: 16, padding: 24, width: '90%', maxWidth: 450, margin: 20 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, margin: 0 }}>
                🎁 {showDeliver.hijo.nombreHijo}
              </h3>
              <button onClick={() => setShowDeliver(null)} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#9ca3af' }}>
                <X className="w-5 h-5" />
              </button>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              {/* Juguete sugerido */}
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 4, display: 'block' }}>Juguete</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                  {juguetesDisponibles.filter(j => j.categoria === showDeliver.hijo.categoria && (j.genero === showDeliver.hijo.generoHijo || j.genero === 'TODOS')).length > 1 ? (
                    // Multiple options - show select
                    <select value={jugueteIdDeliver} onChange={e => setJugueteIdDeliver(parseInt(e.target.value))}
                      style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14 }}>
                      {juguetesDisponibles.filter(j => j.categoria === showDeliver.hijo.categoria && (j.genero === showDeliver.hijo.generoHijo || j.genero === 'TODOS')).map(j => (
                        <option key={j.id} value={j.id} disabled={j.stockActual <= 0}>
                          {j.nombreJuguete} (Stock: {j.stockActual}){j.stockActual <= 0 ? ' AGOTADO' : ''}
                        </option>
                      ))}
                    </select>
                  ) : (
                    <p style={{ fontWeight: 600, margin: 0 }}>
                      {showDeliver.hijo.jugueteSugerido?.nombreJuguete || 'Sin sugerencia'}
                      <span style={{ marginLeft: 8, fontSize: 11, color: showDeliver.hijo.jugueteSugerido?.stockActual ? '#10b981' : '#ef4444' }}>
                        (Stock: {showDeliver.hijo.jugueteSugerido?.stockActual || 0})
                      </span>
                    </p>
                  )}
                  {(() => {
                    const selJuguete = juguetesDisponibles.find(j => j.id === jugueteIdDeliver) || showDeliver.hijo.jugueteSugerido;
                    return selJuguete?.fotoUrl ? (
                      <img src={selJuguete.fotoUrl} alt="" style={{ width: '100%', maxHeight: 120, borderRadius: 8, objectFit: 'contain', border: '1px solid #e5e7eb', marginTop: 8, background: '#f8fafc' }} />
                    ) : null;
                  })()}
                </div>
              </div>

              {/* Receptor */}
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: '#6b7280', marginBottom: 6, display: 'block' }}>Recibido por</label>
                <div style={{ display: 'flex', gap: 8 }}>
                  {['COLABORADOR', 'CONYUGE', 'TERCERO'].map((r) => (
                    <button key={r} onClick={() => { setRecibidoPor(r); setNombreReceptor(''); }}
                      style={{ flex: 1, padding: '8px 12px', borderRadius: 8, border: 'none', fontWeight: 600, fontSize: 12, cursor: 'pointer',
                        ...(recibidoPor === r ? { background: '#da121a', color: 'white' } : { background: '#f3f4f6', color: '#6b7280' })
                      }}>
                      {r === 'COLABORADOR' ? 'Colaborador' : r === 'CONYUGE' ? 'Cónyuge' : 'Tercero'}
                    </button>
                  ))}
                </div>
                {recibidoPor === 'TERCERO' && (
                  <input type="text" value={nombreReceptor} onChange={(e) => setNombreReceptor(e.target.value)}
                    placeholder="Nombre de quien recibe"
                    style={{ marginTop: 8, width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14 }} />
                )}
              </div>

              {/* Foto de evidencia dentro del modal */}
              <div style={{ padding: 14, border: deliverFoto ? '2px solid #10b981' : '2px solid #ef4444', borderRadius: 10, background: '#f8fafc' }}>
                <p style={{ fontSize: 11, fontWeight: 700, color: '#374151', margin: '0 0 6px' }}>
                  📸 Foto de Evidencia <span style={{ color: '#ef4444' }}>(Requerido)</span>
                </p>
                <div style={{ display: 'flex', gap: 8 }}>
                  <label style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer', padding: '6px 10px', borderRadius: 6, border: '1px dashed #d1d5db', fontSize: 12 }}>
                    <Camera className="w-4 h-4" style={{ color: '#9ca3af' }} />
                    <span style={{ color: '#6b7280' }}>{deliverFoto ? deliverFoto.name : 'Subir foto'}</span>
                    <input type="file" accept="image/*" style={{ display: 'none' }} onChange={(e) => setDeliverFoto(e.target.files?.[0] || null)} />
                  </label>
                  <button onClick={() => { setShowCam(true); setTimeout(startCam, 100); }}
                    style={{ padding: '6px 10px', borderRadius: 6, border: '1px dashed #d1d5db', background: 'white', cursor: 'pointer', fontSize: 12, fontWeight: 600, color: '#374151' }}>
                    📷 Cámara
                  </button>
                </div>
                {deliverFoto && (
                  <img src={URL.createObjectURL(deliverFoto)} alt="Preview" style={{ width: '100%', maxHeight: 100, borderRadius: 6, objectFit: 'cover', marginTop: 6 }} />
                )}
              </div>

              <button onClick={async () => {
                if (!colaboradorFoto && !deliverFoto) {
                  Swal.fire({
                    icon: 'error',
                    title: 'Foto requerida',
                    text: 'Debe tomar o subir una foto de evidencia antes de entregar.',
                    confirmButtonText: 'Entendido',
                    confirmButtonColor: '#da121a',
                  });
                  return;
                }
                const fotoFinal = (deliverFoto || colaboradorFoto) || undefined;
                handleEntrega(
                  showDeliver.hijo.id,
                  jugueteIdDeliver || showDeliver.hijo.jugueteSugerido?.id || 0,
                  recibidoPor,
                  recibidoPor === 'TERCERO' ? nombreReceptor : null,
                  ficha.colaborador.carnet,
                  fotoFinal
                );
              }}
                style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 8, padding: 12, fontWeight: 700, fontSize: 14, cursor: 'pointer' }}>
                <Gift className="w-4 h-4" style={{ verticalAlign: 'middle', marginRight: 6 }} /> Confirmar Entrega
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Revert Dialog */}
      {showRevert && (
        <div style={{
          position: 'fixed', inset: 0, zIndex: 50, display: 'flex', alignItems: 'center', justifyContent: 'center',
          background: 'rgba(0,0,0,0.4)'
        }} onClick={() => setShowRevert(null)}>
          <div onClick={(e) => e.stopPropagation()} style={{ background: 'white', borderRadius: 16, padding: 24, width: '90%', maxWidth: 400, margin: 20 }}>
            <h3 style={{ fontFamily: "'Outfit', sans-serif", fontWeight: 700, marginBottom: 8 }}>↺ Reversar entrega</h3>
            <p style={{ fontSize: 14, color: '#6b7280', marginBottom: 16 }}>
              ¿Reversar entrega de <strong style={{ color: '#1f2937' }}>{showRevert.hijo.nombreHijo}</strong>?
            </p>
            <RevertForm onConfirm={(motivo) => handleRevert(showRevert.entregaId, motivo)} onClose={() => setShowRevert(null)} />
          </div>
        </div>
      )}

      {/* Foto preview modal */}
      {fotoPreview && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(0,0,0,0.92)', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}
          onClick={() => setFotoPreview(null)}>
          <div style={{ position: 'absolute', top: 0, left: 0, right: 0, display: 'flex', justifyContent: 'flex-end', padding: 16, zIndex: 1 }}>
            <button onClick={() => setFotoPreview(null)}
              style={{ background: '#ef4444', border: 'none', color: 'white', borderRadius: 8, padding: '8px 18px', fontSize: 14, fontWeight: 700, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: 6, boxShadow: '0 4px 12px rgba(0,0,0,0.3)' }}>
              <X className="w-4 h-4" /> Cerrar
            </button>
          </div>
          <img src={fotoPreview} alt="Evidencia" style={{ maxWidth: '90%', maxHeight: '85%', borderRadius: 8, objectFit: 'contain', cursor: 'pointer' }}
            onClick={(e) => e.stopPropagation()} />
        </div>
      )}
    </div>
  );
}

function RevertForm({ onConfirm, onClose }: { onConfirm: (motivo: string) => void; onClose: () => void }) {
  const [motivo, setMotivo] = useState('');
  return (
    <>
      <textarea value={motivo} onChange={(e) => setMotivo(e.target.value)}
        placeholder="Motivo de la reversión (mín. 10 caracteres)"
        style={{ width: '100%', padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', fontSize: 14, resize: 'vertical', height: 80, marginBottom: 16 }} />
      <div style={{ display: 'flex', gap: 12 }}>
        <button onClick={onClose} style={{ flex: 1, padding: 10, borderRadius: 8, border: '1px solid #e5e7eb', background: 'white', fontWeight: 600, fontSize: 13, cursor: 'pointer' }}>Cancelar</button>
        <button onClick={() => motivo.length >= 10 && onConfirm(motivo)} disabled={motivo.length < 10}
          style={{ flex: 1, padding: 10, borderRadius: 8, border: 'none', fontWeight: 600, fontSize: 13, cursor: motivo.length >= 10 ? 'pointer' : 'not-allowed',
            ...(motivo.length >= 10 ? { background: '#dc2626', color: 'white' } : { background: '#e5e7eb', color: '#9ca3af' }) }}>
          Confirmar
        </button>
      </div>
    </>
  );
}
