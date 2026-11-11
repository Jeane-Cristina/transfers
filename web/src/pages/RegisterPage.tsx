import { useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link, Navigate, useNavigate } from 'react-router';
import { ApiError } from '../api/client';
import { useAuth } from '../components/Auth';
import { Callout } from '../components/Callout';
import { BrandMark, Icon } from '../components/Icon';
import { PasswordInput } from '../components/PasswordInput';
import { ThemeToggle } from '../components/Theme';
import { useToast } from '../components/Toast';
import { formatCpf, isValidCpf, onlyDigits } from '../lib/cpf';
import { firstName, money } from '../lib/format';
import { useDocumentTitle } from '../lib/hooks';

const schema = z
  .object({
    holderName: z
      .string()
      .transform((v) => v.trim().replace(/\s+/g, ' '))
      .refine((v) => v.length >= 3 && v.includes(' '), 'Informe nome e sobrenome.'),
    document: z.string().refine(isValidCpf, 'CPF inválido. Confira os números.'),
    password: z
      .string()
      .min(8, 'Use ao menos 8 caracteres.')
      .refine((v) => /[A-Za-zÀ-ÿ]/.test(v) && /\d/.test(v), 'Misture letras e números.'),
    confirm: z.string(),
  })
  .refine((d) => d.password === d.confirm, { path: ['confirm'], message: 'As senhas não conferem.' });

type RegisterInput = z.input<typeof schema>;
type RegisterValues = z.output<typeof schema>;

export function RegisterPage() {
  useDocumentTitle('Criar conta');
  const { session, register: registerAccount } = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const [error, setError] = useState<string | null>(null);

  const {
    control,
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterInput, unknown, RegisterValues>({
    resolver: zodResolver(schema),
    defaultValues: { holderName: '', document: '', password: '', confirm: '' },
  });

  const password = useWatch({ control, name: 'password' }) ?? '';
  const rules = [
    { ok: password.length >= 8, text: 'Ao menos 8 caracteres' },
    { ok: /[A-Za-zÀ-ÿ]/.test(password), text: 'Uma letra' },
    { ok: /\d/.test(password), text: 'Um número' },
  ];

  if (session) return <Navigate to="/" replace />;

  const onSubmit = async (values: RegisterValues) => {
    setError(null);
    try {
      const response = await registerAccount(values.holderName, onlyDigits(values.document), values.password);
      toast({
        tone: 'success',
        title: `Conta criada. Bem-vindo, ${firstName(response.account.holderName)}!`,
        description: `Saldo inicial de ${money(response.account.balance)} e cheque especial de ${money(response.account.overdraftLimit)}.`,
      });
      navigate('/', { replace: true });
    } catch (e) {
      setError(e instanceof ApiError || e instanceof Error ? e.message : 'Não foi possível criar a conta.');
    }
  };

  return (
    <div className="login">
      <section className="login__aside" aria-hidden="true">
        <div className="login__brand">
          <BrandMark />
          <span>Transferências</span>
        </div>
        <div className="login__pitch">
          <p className="login__headline">Abra sua conta em menos de um minuto.</p>
          <ul className="login__points">
            <li>Só nome, CPF e uma senha</li>
            <li>Cheque especial disponível desde o primeiro dia</li>
            <li>Transferências na hora ou agendadas</li>
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
          <Link className="band__back" to="/entrar">
            <Icon name="chevronLeft" size={18} />
            Entrar
          </Link>
          <h1>Criar conta</h1>
          <p className="login__intro">Preencha seus dados. Você já entra logado no final.</p>

          <form className="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="field">
              <label htmlFor="holderName">Nome completo</label>
              <input
                id="holderName"
                className="text-input"
                autoComplete="name"
                {...register('holderName')}
                aria-invalid={errors.holderName ? true : undefined}
                aria-describedby={errors.holderName ? 'holderName-error' : undefined}
              />
              {errors.holderName && (
                <p id="holderName-error" className="field__error">
                  {errors.holderName.message}
                </p>
              )}
            </div>

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
                autoComplete="new-password"
                {...register('password')}
                aria-invalid={errors.password ? true : undefined}
                aria-describedby="password-rules"
              />
              <ul id="password-rules" className="password-rules">
                {rules.map((rule) => (
                  <li key={rule.text} className={rule.ok ? 'is-ok' : undefined}>
                    <Icon name={rule.ok ? 'check' : 'x'} size={14} />
                    {rule.text}
                  </li>
                ))}
              </ul>
            </div>

            <div className="field">
              <label htmlFor="confirm">Confirme a senha</label>
              <PasswordInput
                id="confirm"
                autoComplete="new-password"
                {...register('confirm')}
                aria-invalid={errors.confirm ? true : undefined}
                aria-describedby={errors.confirm ? 'confirm-error' : undefined}
              />
              {errors.confirm && (
                <p id="confirm-error" className="field__error">
                  {errors.confirm.message}
                </p>
              )}
            </div>

            {error && <Callout tone="error" title="Não foi possível criar a conta">{error}</Callout>}

            <button type="submit" className="btn btn--primary btn--block" disabled={isSubmitting}>
              {isSubmitting ? 'Criando conta…' : 'Criar conta'}
            </button>
          </form>

          <p className="login__switch">
            Já tem conta? <Link to="/entrar">Entrar</Link>
          </p>
        </div>
      </main>
    </div>
  );
}
