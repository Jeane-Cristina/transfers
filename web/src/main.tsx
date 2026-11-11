import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router';
import { ApiError } from './api/client';
import { AuthProvider } from './components/Auth';
import { HideValuesProvider } from './components/HideValues';
import { ThemeProvider } from './components/Theme';
import { ToastProvider } from './components/Toast';
import { router } from './router';
import './styles/base.css';
import './styles/components.css';
import './styles/pages.css';
import './styles/home.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5_000,
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status >= 400 && error.status < 500) && failureCount < 2,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <HideValuesProvider>
          <ToastProvider>
            <RouterProvider router={router} />
          </ToastProvider>
        </HideValuesProvider>
      </AuthProvider>
    </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>,
);
