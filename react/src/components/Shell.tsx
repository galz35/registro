import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { LayoutDashboard, UserCheck, Gift, Package, FileText, Upload, LogOut, Gift as GiftIcon, Menu, X } from 'lucide-react';

const navItems = [
  { label: 'Dashboard', icon: LayoutDashboard, to: '/' },
  { label: 'Registro Asistencia', icon: UserCheck, to: '/attendance' },
  { label: 'Despacho', icon: Gift, to: '/dispatch' },
  { label: 'Catálogo', icon: Package, to: '/catalog' },
  { label: 'Historial', icon: FileText, to: '/history' },
  { label: 'Importar', icon: Upload, to: '/import' },
];

export default function Shell({ children }: { children: React.ReactNode }) {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="app-layout">
      {sidebarOpen && <div className="app-overlay lg:hidden" onClick={() => setSidebarOpen(false)} />}

      <aside className={`app-sidebar ${sidebarOpen ? 'is-open' : ''}`}>
        <div className="app-sidebar__header">
          <div className="app-brand">
            <div className="app-brand__mark">
              <GiftIcon className="w-5 h-5" />
            </div>
            <div>
              <p className="app-brand__title">Asistencia</p>
              <p className="app-brand__subtitle">Día del Niño</p>
            </div>
          </div>
          <button onClick={() => setSidebarOpen(false)} className="app-menu-button app-menu-button--light app-sidebar__close" title="Cerrar menú">
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="app-nav">
          {navItems.map((item) => {
            const active = location.pathname === item.to;
            return (
              <Link
                key={item.to}
                to={item.to}
                onClick={() => setSidebarOpen(false)}
                className={`app-nav__link ${active ? 'is-active' : ''}`}
              >
                <item.icon className="w-5 h-5" />
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="app-user">
          <div className="app-user__card">
            <div className="app-user__avatar">
              {user?.nombre?.charAt(0) || 'U'}
            </div>
            <div style={{ minWidth: 0 }}>
              <div className="app-user__name">{user?.nombre || 'Usuario'}</div>
              <div className="app-user__role">{user?.rol || '-'}</div>
            </div>
            <button onClick={logout} className="app-menu-button app-menu-button--light" title="Salir">
              <LogOut className="w-4 h-4" />
            </button>
          </div>
        </div>
      </aside>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 16px', borderBottom: '1px solid #e5e7eb', background: 'white' }}>
          <button onClick={() => setSidebarOpen(true)} style={{ background: 'none', border: 'none', color: '#6b7280', cursor: 'pointer', padding: 4 }}>
            <Menu className="w-5 h-5" />
          </button>
          <div style={{ fontSize: 14, fontWeight: 700, color: '#1f2937' }}>Asistencia</div>
        </div>
        {children}
      </div>
    </div>
  );
}
