import { ReactNode, ButtonHTMLAttributes } from 'react';
import { useAuth } from '../hooks/useAuth';

interface LogoutButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'onClick'> {
  onLogoutStart?: () => void;
  onLogoutComplete?: () => void;
  onLogoutError?: (error: Error) => void;
  children?: ReactNode;
  loadingText?: string;
  redirectAfterLogout?: boolean;
}

export function LogoutButton({
  onLogoutStart,
  onLogoutComplete,
  onLogoutError,
  children,
  loadingText = 'Logging out...',
  redirectAfterLogout = false,
  disabled,
  className,
  ...buttonProps
}: LogoutButtonProps) {
  const { logout, isLoading, isAuthenticated } = useAuth();

  const handleClick = async () => {
    try {
      onLogoutStart?.();
      const result = await logout();
      
      if (result.loggedOut) {
        onLogoutComplete?.();
        
        if (redirectAfterLogout && result.redirectUrl) {
          window.location.href = result.redirectUrl;
        }
      }
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Logout failed');
      onLogoutError?.(error);
    }
  };

  // Don't show logout button if not authenticated
  if (!isAuthenticated) {
    return null;
  }

  const isDisabled = disabled || isLoading;
  const displayText = isLoading ? loadingText : children;

  return (
    <button
      onClick={handleClick}
      disabled={isDisabled}
      className={className}
      aria-busy={isLoading}
      {...buttonProps}
    >
      {displayText || 'Logout'}
    </button>
  );
}