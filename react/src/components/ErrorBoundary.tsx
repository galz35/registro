import { Component } from 'react';
import type { ReactNode, ErrorInfo } from 'react';

interface Props { children: ReactNode; }
interface State { hasError: boolean; error: Error | null; stack: string; }

export default class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, stack: '' };
  }

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error, stack: '' };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('ERROR:', error.message);
    console.error('STACK:', error.stack);
    console.error('COMPONENT:', info.componentStack);
    this.setState({ stack: info.componentStack || '' });
  }

  render() {
    if (this.state.hasError) {
      const stackLines = this.state.stack.split('\n').filter(l => l.trim()).slice(0, 5).join('\n');
      return (
        <div className="min-h-screen flex items-center justify-center p-8" style={{ background: '#f8f9fa' }}>
          <div className="card p-6" style={{ maxWidth: 600, width: '100%' }}>
            <h2 style={{ fontFamily: "'Outfit', sans-serif", color: '#dc2626', marginBottom: 8 }}>Error en la aplicación</h2>
            <p style={{ color: '#374151', marginBottom: 16, fontFamily: 'monospace', fontSize: 13, background: '#f1f5f9', padding: 12, borderRadius: 8, border: '1px solid #e2e8f0' }}>
              {this.state.error?.message || 'Error desconocido'}
            </p>
            {stackLines && (
              <details style={{ marginBottom: 16 }}>
                <summary style={{ fontSize: 12, color: '#6b7280', cursor: 'pointer' }}>Ver detalle técnico</summary>
                <pre style={{ fontSize: 11, color: '#6b7280', marginTop: 8, whiteSpace: 'pre-wrap', maxHeight: 200, overflow: 'auto' }}>
                  {this.state.error?.stack}
                </pre>
              </details>
            )}
            <button
              onClick={() => window.location.href = '/asistencia/'}
              style={{ background: '#da121a', color: 'white', border: 'none', borderRadius: 8, padding: '10px 24px', fontWeight: 600, fontSize: 14, cursor: 'pointer' }}
            >
              Recargar
            </button>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
