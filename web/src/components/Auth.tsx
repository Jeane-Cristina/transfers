import { createContext, useCallback, useContext, useMemo, useSyncExternalStore, type ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Navigate, useLocation } from 'react-router';
import { api } from '../api/client';
import { sessionStore, type Session } from '../api/session';
import type { LoginResponse } from '../api/types';

interface AuthContextValue {
  session: Session | null;
  login: (document: string, password: string) => Promise<Session>;
  register: (holderName: string, document: string, password: string) => Promise<LoginResponse>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const session = useSyncExternalStore(sessionStore.subscribe, sessionStore.get);

  const start = useCallback(
    (response: LoginResponse) => {
      const next: Session = {
        token: response.token,
        expiresAt: response.expiresAt,
        accountId: response.account.id,
        holderName: response.account.holderName,
      };
      queryClient.clear();
      queryClient.setQueryData(['accounts', next.accountId], response.account);
      sessionStore.set(next);
      return next;
    },
    [queryClient],
  );

  const login = useCallback(
    async (document: string, password: string) => start(await api.login(document, password)),
    [start],
  );

  const register = useCallback(
    async (holderName: string, document: string, password: string) => {
      const response = await api.register(holderName, document, password);
      start(response);
      return response;
    },
    [start],
  );

  const logout = useCallback(() => {
    sessionStore.set(null);
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo(() => ({ session, login, register, logout }), [session, login, register, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth precisa estar dentro de AuthProvider.');
  return context;
}

export function useSession(): Session {
  const { session } = useAuth();
  if (!session) throw new Error('Sessão ausente.');
  return session;
}

export function RequireAuth({ children }: { children: ReactNode }) {
  const { session } = useAuth();
  const location = useLocation();
  if (!session) {
    const next = encodeURIComponent(location.pathname + location.search);
    return <Navigate to={`/entrar?next=${next}`} replace />;
  }
  return <>{children}</>;
}
