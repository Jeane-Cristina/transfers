import { Link, Outlet, ScrollRestoration, useLocation, useNavigate } from 'react-router';
import { initials } from '../lib/format';
import { useAuth } from './Auth';
import { BrandMark, Icon } from './Icon';
import { ThemeToggle } from './Theme';

export function Layout() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const { session, logout } = useAuth();
  const homeActive = pathname === '/';
  const newActive = pathname === '/transfers/new';
  const statementActive = pathname === '/extrato';

  const signOut = () => {
    logout();
    navigate('/entrar', { replace: true });
  };

  return (
    <>
      <a className="skip-link" href="#main">
        Pular para o conteúdo
      </a>
      <header className="topbar">
        <div className="topbar__inner">
          <Link to="/" className="brand">
            <BrandMark />
            <span>Transferências</span>
          </Link>
          <nav className="nav" aria-label="Principal">
            <Link to="/" className={homeActive ? 'is-active' : undefined} aria-current={homeActive ? 'page' : undefined}>
              Início
            </Link>
            <Link to="/transfers/new" className={newActive ? 'is-active' : undefined} aria-current={newActive ? 'page' : undefined}>
              Transferir
            </Link>
            <Link to="/extrato" className={statementActive ? 'is-active' : undefined} aria-current={statementActive ? 'page' : undefined}>
              Extrato
            </Link>
          </nav>
          <ThemeToggle className="icon-btn topbar__theme" />
          {session && (
            <div className="user">
              <span className="avatar avatar--sm" aria-hidden="true">{initials(session.holderName)}</span>
              <span className="user__name">{session.holderName}</span>
              <button type="button" className="btn btn--ghost btn--sm user__logout" onClick={signOut}>
                Sair
              </button>
            </div>
          )}
        </div>
      </header>
      <main id="main" tabIndex={-1}>
        <Outlet />
      </main>
      <nav className="bottom-nav" aria-label="Principal">
        <Link to="/" className={homeActive ? 'is-active' : undefined} aria-current={homeActive ? 'page' : undefined}>
          <Icon name="home" size={22} />
          Início
        </Link>
        <Link to="/transfers/new" className={newActive ? 'is-active' : undefined} aria-current={newActive ? 'page' : undefined}>
          <Icon name="send" size={22} />
          Transferir
        </Link>
        <Link to="/extrato" className={statementActive ? 'is-active' : undefined} aria-current={statementActive ? 'page' : undefined}>
          <Icon name="wallet" size={22} />
          Extrato
        </Link>
        <button type="button" onClick={signOut}>
          <Icon name="logout" size={22} />
          Sair
        </button>
      </nav>
      <ScrollRestoration />
    </>
  );
}
