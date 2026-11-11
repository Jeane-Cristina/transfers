import { createBrowserRouter } from 'react-router';
import { RequireAuth } from './components/Auth';
import { Layout } from './components/Layout';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { NewTransferPage } from './pages/NewTransferPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { RegisterPage } from './pages/RegisterPage';
import { StatementPage } from './pages/StatementPage';
import { TransferDetailPage } from './pages/TransferDetailPage';

export const router = createBrowserRouter([
  { path: '/entrar', element: <LoginPage /> },
  { path: '/criar-conta', element: <RegisterPage /> },
  {
    element: (
      <RequireAuth>
        <Layout />
      </RequireAuth>
    ),
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/extrato', element: <StatementPage /> },
      { path: '/transfers/new', element: <NewTransferPage /> },
      { path: '/transfers/:id', element: <TransferDetailPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
