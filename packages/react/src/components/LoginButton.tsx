import { ReactNode, ButtonHTMLAttributes } from 'react';
import { useAuth } from '../hooks/useAuth';

interface LoginButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'onClick'> {
  provider: string;
  returnUrl?: string;
  onLoginStart?: () => void;
  onLoginError?: (error: Error) => void;
  children?: ReactNode;
  loadingText?: string;
  errorText?: string;
}

export function LoginButton({
  provider,
  returnUrl,
  onLoginStart,
  onLoginError,
  children,
  loadingText = 'Logging in...',
  errorText = 'Login failed',
  disabled,
  className,
  ...buttonProps
}: LoginButtonProps) {
  const { login, isLoading, error } = useAuth();

  const handleClick = async () => {
    try {
      onLoginStart?.();
      await login(provider, returnUrl);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Login failed');
      onLoginError?.(error);
    }
  };

  const isDisabled = disabled || isLoading;
  const displayText = isLoading ? loadingText : (error ? errorText : children);

  return (
    <button
      onClick={handleClick}
      disabled={isDisabled}
      className={className}
      aria-busy={isLoading}
      {...buttonProps}
    >
      {displayText || `Login with ${provider}`}
    </button>
  );
}

interface GoogleLoginButtonProps extends Omit<LoginButtonProps, 'provider'> {}

export function GoogleLoginButton(props: GoogleLoginButtonProps) {
  return (
    <LoginButton
      provider="Google"
      {...props}
    >
      {props.children || 'Continue with Google'}
    </LoginButton>
  );
}

interface FacebookLoginButtonProps extends Omit<LoginButtonProps, 'provider'> {}

export function FacebookLoginButton(props: FacebookLoginButtonProps) {
  return (
    <LoginButton
      provider="Facebook"
      {...props}
    >
      {props.children || 'Continue with Facebook'}
    </LoginButton>
  );
}

interface AppleLoginButtonProps extends Omit<LoginButtonProps, 'provider'> {}

export function AppleLoginButton(props: AppleLoginButtonProps) {
  return (
    <LoginButton
      provider="Apple"
      {...props}
    >
      {props.children || 'Continue with Apple'}
    </LoginButton>
  );
}

interface AzureB2CLoginButtonProps extends Omit<LoginButtonProps, 'provider'> {}

export function AzureB2CLoginButton(props: AzureB2CLoginButtonProps) {
  return (
    <LoginButton
      provider="AzureB2C"
      {...props}
    >
      {props.children || 'Continue with Microsoft'}
    </LoginButton>
  );
}