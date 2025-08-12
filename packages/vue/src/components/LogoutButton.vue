<template>
  <button
    v-if="isAuthenticated"
    :disabled="disabled || isLoading"
    :class="buttonClass"
    :aria-busy="isLoading"
    @click="handleClick"
  >
    <slot v-if="$slots.default" />
    <template v-else>
      {{ displayText }}
    </template>
  </button>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useAuth } from '../composables/useAuth';

interface Props {
  disabled?: boolean;
  loadingText?: string;
  class?: string | string[] | Record<string, boolean>;
  redirectAfterLogout?: boolean;
  onLogoutStart?: () => void;
  onLogoutComplete?: () => void;
  onLogoutError?: (error: Error) => void;
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  loadingText: 'Logging out...',
  class: '',
  redirectAfterLogout: false,
});

const emit = defineEmits<{
  logoutStart: [];
  logoutComplete: [];
  logoutError: [error: Error];
}>();

const auth = useAuth();

const buttonClass = computed(() => {
  const classes = [];
  
  if (typeof props.class === 'string') {
    classes.push(props.class);
  } else if (Array.isArray(props.class)) {
    classes.push(...props.class);
  } else if (typeof props.class === 'object') {
    for (const [key, value] of Object.entries(props.class)) {
      if (value) classes.push(key);
    }
  }
  
  return classes.join(' ');
});

const displayText = computed(() => {
  if (auth.isLoading) {
    return props.loadingText;
  }
  return 'Logout';
});

const handleClick = async () => {
  try {
    props.onLogoutStart?.();
    emit('logoutStart');
    
    const result = await auth.logout();
    
    if (result.loggedOut) {
      props.onLogoutComplete?.();
      emit('logoutComplete');
      
      if (props.redirectAfterLogout && result.redirectUrl) {
        window.location.href = result.redirectUrl;
      }
    }
  } catch (error) {
    const err = error instanceof Error ? error : new Error('Logout failed');
    props.onLogoutError?.(err);
    emit('logoutError', err);
  }
};
</script>

<style scoped>
.easyauth-logout-button {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  border: 1px solid #dc2626;
  border-radius: 6px;
  background-color: transparent;
  color: #dc2626;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
  text-decoration: none;
  min-height: 36px;
}

.easyauth-logout-button:hover:not(:disabled) {
  background-color: #dc2626;
  color: white;
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(220, 38, 38, 0.2);
}

.easyauth-logout-button:focus {
  outline: 2px solid #dc2626;
  outline-offset: 2px;
}

.easyauth-logout-button:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 1px 2px rgba(220, 38, 38, 0.2);
}

.easyauth-logout-button:disabled,
.easyauth-logout-button.loading {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.easyauth-logout-button.loading {
  cursor: wait;
}

/* Loading spinner */
.loading-indicator {
  display: inline-flex;
  align-items: center;
}

.spinner {
  width: 14px;
  height: 14px;
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>