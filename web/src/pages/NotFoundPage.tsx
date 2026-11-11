import { Link } from 'react-router';
import { Icon } from '../components/Icon';
import { useDocumentTitle } from '../lib/hooks';

export function NotFoundPage() {
  useDocumentTitle('Página não encontrada');
  return (
    <div className="page">
      <div className="state">
        <span className="state__icon state__icon--neutral">
          <Icon name="info" size={28} />
        </span>
        <h2>Página não encontrada</h2>
        <p>O endereço pode estar incorreto ou a página não existe mais.</p>
        <div className="state__actions">
          <Link className="btn btn--primary" to="/">
            Ir para o início
          </Link>
        </div>
      </div>
    </div>
  );
}
