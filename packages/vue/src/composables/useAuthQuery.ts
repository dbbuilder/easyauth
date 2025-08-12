import { ref, watch, onMounted, onUnmounted } from 'vue';
import type { UseAuthQueryOptions, UseAuthQueryReturn } from '../types';
import { authApi } from '../utils/api';

export function useAuthQuery<T>(
  queryFn: () => Promise<T>,
  options: UseAuthQueryOptions<T> = {}
): UseAuthQueryReturn<T> {
  const data = ref<T | undefined>(undefined);
  const error = ref<Error | null>(null);
  const isLoading = ref(false);
  const isError = ref(false);
  const isSuccess = ref(false);

  const {
    enabled = true,
    refetchOnWindowFocus = false,
    onError,
    onSuccess,
  } = options;

  const execute = async () => {
    if (!enabled) return;

    isLoading.value = true;
    error.value = null;
    isError.value = false;
    isSuccess.value = false;

    try {
      const result = await queryFn();
      data.value = result;
      isSuccess.value = true;
      onSuccess?.(result);
    } catch (err) {
      const errorObj = err instanceof Error ? err : new Error('Query failed');
      error.value = errorObj;
      isError.value = true;
      onError?.(errorObj);
    } finally {
      isLoading.value = false;
    }
  };

  const refetch = async () => {
    await execute();
  };

  // Initial execution
  onMounted(() => {
    if (enabled) {
      execute();
    }
  });

  // Watch enabled state
  watch(() => enabled, (newEnabled) => {
    if (newEnabled) {
      execute();
    }
  });

  // Refetch on window focus
  let focusHandler: (() => void) | null = null;
  let visibilityHandler: (() => void) | null = null;

  if (refetchOnWindowFocus) {
    focusHandler = () => {
      if (!document.hidden) {
        execute();
      }
    };

    visibilityHandler = () => {
      if (!document.hidden) {
        execute();
      }
    };

    onMounted(() => {
      window.addEventListener('focus', focusHandler!);
      document.addEventListener('visibilitychange', visibilityHandler!);
    });

    onUnmounted(() => {
      if (focusHandler) window.removeEventListener('focus', focusHandler);
      if (visibilityHandler) document.removeEventListener('visibilitychange', visibilityHandler);
    });
  }

  return {
    data,
    error,
    isLoading,
    isError,
    isSuccess,
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