import { useEffect, useState } from 'react';
import { authApi } from '../utils/api';

interface UseAuthQueryOptions<T> {
  enabled?: boolean;
  refetchOnWindowFocus?: boolean;
  staleTime?: number;
  cacheTime?: number;
  onError?: (error: any) => void;
  onSuccess?: (data: T) => void;
}

interface UseAuthQueryReturn<T> {
  data: T | undefined;
  error: Error | null;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  refetch: () => Promise<void>;
}

export function useAuthQuery<T>(
  queryFn: () => Promise<T>,
  options: UseAuthQueryOptions<T> = {}
): UseAuthQueryReturn<T> {
  const [data, setData] = useState<T | undefined>(undefined);
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const {
    enabled = true,
    refetchOnWindowFocus = false,
    onError,
    onSuccess,
  } = options;

  const execute = async () => {
    if (!enabled) return;

    setIsLoading(true);
    setError(null);

    try {
      const result = await queryFn();
      setData(result);
      onSuccess?.(result);
    } catch (err) {
      const error = err instanceof Error ? err : new Error('Query failed');
      setError(error);
      onError?.(error);
    } finally {
      setIsLoading(false);
    }
  };

  const refetch = async () => {
    await execute();
  };

  useEffect(() => {
    execute();
  }, [enabled]);

  useEffect(() => {
    if (!refetchOnWindowFocus) return;

    const handleFocus = () => {
      if (!document.hidden) {
        execute();
      }
    };

    window.addEventListener('focus', handleFocus);
    document.addEventListener('visibilitychange', handleFocus);

    return () => {
      window.removeEventListener('focus', handleFocus);
      document.removeEventListener('visibilitychange', handleFocus);
    };
  }, [refetchOnWindowFocus]);

  return {
    data,
    error,
    isLoading,
    isError: !!error,
    isSuccess: !!data && !error,
    refetch,
  };
}

export function useUserProfile() {
  return useAuthQuery(() => authApi.getUserProfile(), {
    enabled: true,
    refetchOnWindowFocus: true,
  });
}

export function useHealthCheck() {
  return useAuthQuery(() => authApi.healthCheck(), {
    enabled: true,
    staleTime: 30000, // 30 seconds
  });
}