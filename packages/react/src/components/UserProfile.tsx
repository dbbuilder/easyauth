import { ReactNode } from 'react';
import { useAuth } from '../hooks/useAuth';
import { UserInfo } from '../types';

interface UserProfileProps {
  children?: (user: UserInfo) => ReactNode;
  fallback?: ReactNode;
  showAvatar?: boolean;
  showEmail?: boolean;
  showName?: boolean;
  showRoles?: boolean;
  className?: string;
}

export function UserProfile({
  children,
  fallback,
  showAvatar = true,
  showEmail = true,
  showName = true,
  showRoles = false,
  className,
}: UserProfileProps) {
  const { isAuthenticated, user, isLoading } = useAuth();

  if (isLoading) {
    return fallback || <div>Loading profile...</div>;
  }

  if (!isAuthenticated || !user) {
    return fallback || null;
  }

  // If children is a function, use render prop pattern
  if (typeof children === 'function') {
    return <>{children(user)}</>;
  }

  // Default profile display
  return (
    <div className={className}>
      {showAvatar && user.profilePicture && (
        <img
          src={user.profilePicture}
          alt={user.name || 'User avatar'}
          style={{ width: 40, height: 40, borderRadius: '50%' }}
        />
      )}
      
      {showName && user.name && (
        <div className="user-name">{user.name}</div>
      )}
      
      {showEmail && user.email && (
        <div className="user-email">{user.email}</div>
      )}
      
      {showRoles && user.roles && user.roles.length > 0 && (
        <div className="user-roles">
          {user.roles.map(role => (
            <span key={role} className="user-role">
              {role}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

interface UserAvatarProps {
  size?: number;
  fallback?: ReactNode;
  className?: string;
  onClick?: () => void;
}

export function UserAvatar({ size = 40, fallback, className, onClick }: UserAvatarProps) {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated || !user) {
    return fallback || null;
  }

  const avatarStyle = {
    width: size,
    height: size,
    borderRadius: '50%',
    cursor: onClick ? 'pointer' : 'default',
  };

  if (user.profilePicture) {
    return (
      <img
        src={user.profilePicture}
        alt={user.name || 'User avatar'}
        style={avatarStyle}
        className={className}
        onClick={onClick}
      />
    );
  }

  // Fallback to initials
  const initials = user.name
    ? user.name
        .split(' ')
        .map(n => n[0])
        .join('')
        .toUpperCase()
        .slice(0, 2)
    : user.email?.[0]?.toUpperCase() || '?';

  return (
    <div
      style={{
        ...avatarStyle,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: '#f0f0f0',
        color: '#333',
        fontSize: size * 0.4,
        fontWeight: 'bold',
      }}
      className={className}
      onClick={onClick}
    >
      {initials}
    </div>
  );
}

interface UserNameProps {
  fallback?: ReactNode;
  className?: string;
}

export function UserName({ fallback, className }: UserNameProps) {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated || !user || !user.name) {
    return fallback || null;
  }

  return <span className={className}>{user.name}</span>;
}

interface UserEmailProps {
  fallback?: ReactNode;
  className?: string;
}

export function UserEmail({ fallback, className }: UserEmailProps) {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated || !user || !user.email) {
    return fallback || null;
  }

  return <span className={className}>{user.email}</span>;
}