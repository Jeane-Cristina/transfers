import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router';
import { ApiError } from '../api/client';
import { useAuth } from '../components/Auth';
import { Callout } from '../components/Callout';
import { BrandMark, Icon } from '../components/Icon';
import { PasswordInput } from '../components/PasswordInput';
import { ThemeToggle } from '../components/Theme';
import { useToast } from '../components/Toast';
import { formatCpf } from '../lib/cpf';
import { initials } from '../lib/format';
import { useDocumentTitle } from '../lib/hooks';

const DEMO_PASSWORD = 'Senha@123';

const DEMO_ACCOUNTS = [
  { name: 'Ana Souza', document: '11111111111', hint: 'Saldo de R$ 500 e cheque especial de R$ 1.000' },
  { name: 'Bruno Lima', document: '22222222222', hint: 'Começa usando R$ 800 do cheque especial' },
  { name: 'Carla Dias', document: '33333333333', hint: 'Saldo alto para testar os limites por hora' },
  { name: 'Diego Rocha', document: '44444444444', hint: 'Conta bloqueada' },
];

const schema = z.object({
  document: z.string().refine((v) => v.replace(/\D/g, '').length === 11, 'Informe os 11 dígitos do CPF.'),
  password: z.string().min(1, 'Informe a senha.'),
});

type LoginValues = z.infer<typeof schema>;

export function LoginPage() {
  useDocumentTitle('Entrar');
  const { session, login } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);

  const {
    control,
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({ resolver: zodResolver(schema), defaultValues: { document: '', password: '' } });

  const next = params.get('next');
  const target = next && next.startsWith('/') && !next.startsWith('//') ? next : '/';

  if (session) return <Navigate to={target} replace />;

  const onSubmit = async (values: LoginValues) => {
    setError(null);
    try {
      const started = await login(values.document.replace(/\D/g, ''), values.password);
      toast({ tone: 'success', title: `Olá, ${started.holderName.split(' ')[0]}` });
      navigate(target, { replace: true });
    } catch (e) {
      setError(
        e instanceof ApiError && e.status === 401
          ? 'Documento ou senha inválidos. Confira os dados e tente de novo.'
          : e instanceof Error
            ? e.message
            : 'Não foi possível entrar.',
      );
    }
  };

  const fillDemo = (document: string) => {
    setValue('document', formatCpf(document), { shouldValidate: true });
    setValue('password', DEMO_PASSWORD, { shouldValidate: true });
    setError(null);
  };

  return (
    <div className="login">
      <section className="login__aside" aria-hidden="true">
        <div className="login__brand">
          <BrandMark />
          <span>Transferências</span>
        </div>
        <div className="login__pitch">
          <p className="login__headline">Envie, agende e acompanhe transferências da sua conta.</p>
          <ul className="login__points">
            <li>Saldo e cheque especial sempre à vista</li>
            <li>Limites por hora conferidos antes de enviar</li>
            <li>Agendamentos que você pode cancelar até a execução</li>
          </ul>
        </div>
      </section>

      <main className="login__main" id="main">
        <ThemeToggle className="icon-btn login__theme" />
        <div className="login__card">
          <div className="login__mobile-brand">
            <BrandMark />
            <span>Transferências</span>
          </div>
          <h1>Entrar</h1>
          <p className="login__intro">Use o CPF do titular e a senha da conta.</p>

          <form className="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="field">
              <label htmlFor="document">CPF</label>
              <Controller
                control={control}
                name="document"
                render={({ field }) => (
                  <input
                    id="document"
                    className="text-input"
                    inputMode="numeric"
                    autoComplete="username"
                    placeholder="000.000.000-00"
                    value={field.value}
                    onChange={(e) => field.onChange(formatCpf(e.target.value))}
                    onBlur={field.onBlur}
                    aria-invalid={errors.document ? true : undefined}
                    aria-describedby={errors.document ? 'document-error' : undefined}
                  />
                )}
              />
              {errors.document && (
                <p id="document-error" className="field__error">
                  {errors.document.message}
                </p>
              )}
            </div>

            <div className="field">
              <label htmlFor="password">Senha</label>
              <PasswordInput
                id="password"
                autoComplete="current-password"
                {...register('password')}
                aria-invalid={errors.password ? true : undefined}
                aria-describedby={errors.password ? 'password-error' : undefined}
              />
              {errors.password && (
                <p id="password-error" className="field__error">
                  {errors.password.message}
                </p>
              )}
            </div>

            {error && <Callout tone="error" title="Não foi possível entrar">{error}</Callout>}

            <button type="submit" className="btn btn--primary btn--block" disabled={isSubmitting}>
              {isSubmitting ? 'Entrando…' : 'Entrar'}
            </button>
          </form>

          <p className="login__switch">
            Ainda não tem conta? <Link to="/criar-conta">Criar conta</Link>
          </p>

          <section className="demo" aria-labelledby="demo-title">
            <h2 id="demo-title">Contas de demonstração</h2>
            <p>
              Todas usam a senha <code>{DEMO_PASSWORD}</code>. Escolha uma para preencher.
            </p>
            <ul className="demo__list">
              {DEMO_ACCOUNTS.map((account) => (
                <li key={account.document}>
                  <button type="button" className="demo__item" onClick={() => fillDemo(account.document)}>
                    <span className="avatar avatar--sm" aria-hidden="true">{initials(account.name)}</span>
                    <span>
                      <strong>{account.name}</strong>
                      <span>{account.hint}</span>
                    </span>
                    <Icon name="chevronRight" size={18} />
                  </button>
                </li>
              ))}
            </ul>
          </section>
        </div>
      </main>
    </div>
  );
}
