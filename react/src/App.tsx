import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import ErrorBoundary from './components/ErrorBoundary';
import Shell from './components/Shell';
import LoginPage from './pages/LoginPage';
import CommandCenter from './pages/CommandCenter';
import SsoHandlerPage from './pages/SsoHandlerPage';
import AttendancePage from './pages/AttendancePage';
import DispatchPage from './pages/DispatchPage';
import CatalogPage from './pages/CatalogPage';
import ImportPage from './pages/ImportPage';
import HistoryPage from './pages/HistoryPage';
import ReportsPage from './pages/ReportsPage';
import AdminPage from './pages/AdminPage';
import type { ReactNode } from 'react';

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

function PublicRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) return <Navigate to="/" replace />;
  return <>{children}</>;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<PublicRoute><LoginPage /></PublicRoute>} />
      <Route path="/auth/sso" element={<SsoHandlerPage />} />
      <Route path="/" element={<ProtectedRoute><Shell><CommandCenter /></Shell></ProtectedRoute>} />
      <Route path="/attendance" element={<ProtectedRoute><Shell><AttendancePage /></Shell></ProtectedRoute>} />
      <Route path="/dispatch" element={<ProtectedRoute><Shell><DispatchPage /></Shell></ProtectedRoute>} />
      <Route path="/catalog" element={<ProtectedRoute><Shell><CatalogPage /></Shell></ProtectedRoute>} />
      <Route path="/import" element={<ProtectedRoute><Shell><ImportPage /></Shell></ProtectedRoute>} />
      <Route path="/history" element={<ProtectedRoute><Shell><HistoryPage /></Shell></ProtectedRoute>} />
      <Route path="/reports" element={<ProtectedRoute><Shell><ReportsPage /></Shell></ProtectedRoute>} />
      <Route path="/admin" element={<ProtectedRoute><Shell><AdminPage /></Shell></ProtectedRoute>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter basename="/asistencia">
      <ErrorBoundary>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </ErrorBoundary>
    </BrowserRouter>
  );
}
