import { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';

interface AuthGuardProps {
  children: ReactNode;
  fallback?: ReactNode;
  requiredRoles?: string[];
  requireAllRoles?: boolean;
  onUnauthorized?: () => void;
  redirectTo?: string;
}

export function AuthGuard({
  children,
  fallback,
  requiredRoles = [],
  requireAllRoles = false,
  onUnauthorized,
  redirectTo,
}: AuthGuardProps) {
  const { isLoading, isAuthenticated, user } = useAuth();

  // Show loading state
  if (isLoading) {
    return fallback || <div>Loading...</div>;
  }

  // Not authenticated
  if (!isAuthenticated) {
    if (redirectTo) {
      window.location.href = redirectTo;
      return null;
    }
    
    if (onUnauthorized) {
      onUnauthorized();
    }
    
    return fallback || <div>Authentication required</div>;
  }

  // Check role requirements
  if (requiredRoles.length > 0 && user) {
    const userRoles = user.roles || [];
    
    const hasRequiredRoles = requireAllRoles
      ? requiredRoles.every(role => userRoles.includes(role))
      : requiredRoles.some(role => userRoles.includes(role));

    if (!hasRequiredRoles) {
      if (onUnauthorized) {
        onUnauthorized();
      }
      
      return fallback || <div>Insufficient permissions</div>;
    }
  }

  // All checks passed
  return <>{children}</>;
}

interface RequireAuthProps {
  children: ReactNode;
  fallback?: ReactNode;
}

export function RequireAuth({ children, fallback }: RequireAuthProps) {
  return (
    <AuthGuard fallback={fallback}>
      {children}
    </AuthGuard>
  );
}

interface RequireRolesProps {
  children: ReactNode;
  roles: string[];
  requireAll?: boolean;
  fallback?: ReactNode;
}

export function RequireRoles({ 
  children, 
  roles, 
  requireAll = false, 
  fallback 
}: RequireRolesProps) {
  return (
    <AuthGuard 
      requiredRoles={roles}
      requireAllRoles={requireAll}
      fallback={fallback}
    >
      {children}
    </AuthGuard>
  );
}