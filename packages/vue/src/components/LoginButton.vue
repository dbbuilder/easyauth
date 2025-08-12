<template>
  <button
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
  provider: string;
  returnUrl?: string;
  disabled?: boolean;
  loadingText?: string;
  errorText?: string;
  class?: string | string[] | Record<string, boolean>;
  onLoginStart?: () => void;
  onLoginError?: (error: Error) => void;
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  loadingText: 'Logging in...',
  errorText: 'Login failed',
  class: '',
});

const emit = defineEmits<{
  loginStart: [];
  loginSuccess: [result: any];
  loginError: [error: Error];
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
  if (auth.error) {
    return props.errorText;
  }
  return `Login with ${props.provider}`;
});

const handleClick = async () => {
  try {
    props.onLoginStart?.();
    emit('loginStart');
    
    const result = await auth.login(props.provider, props.returnUrl);
    emit('loginSuccess', result);
  } catch (error) {
    const err = error instanceof Error ? error : new Error('Login failed');
    props.onLoginError?.(err);
    emit('loginError', err);
  }
};
</script>

<style scoped>
.easyauth-login-button {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 12px 24px;
  border: 1px solid #d1d5db;
  border-radius: 8px;
  background-color: #ffffff;
  color: #374151;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
  text-decoration: none;
  min-height: 44px;
}

.easyauth-login-button:hover:not(:disabled) {
  background-color: #f9fafb;
  border-color: #9ca3af;
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.easyauth-login-button:focus {
  outline: 2px solid #3b82f6;
  outline-offset: 2px;
}

.easyauth-login-button:active:not(:disabled) {
  transform: translateY(0);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.easyauth-login-button:disabled,
.easyauth-login-button.disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.easyauth-login-button.loading {
  cursor: wait;
}

/* Provider-specific styling */
.easyauth-login-button.provider-google {
  border-color: #ea4335;
  color: #ea4335;
}

.easyauth-login-button.provider-google:hover:not(:disabled) {
  background-color: #ea4335;
  color: white;
}

.easyauth-login-button.provider-apple {
  border-color: #000000;
  color: #000000;
}

.easyauth-login-button.provider-apple:hover:not(:disabled) {
  background-color: #000000;
  color: white;
}

.easyauth-login-button.provider-facebook {
  border-color: #1877f2;
  color: #1877f2;
}

.easyauth-login-button.provider-facebook:hover:not(:disabled) {
  background-color: #1877f2;
  color: white;
}

.easyauth-login-button.provider-azure-b2c {
  border-color: #0078d4;
  color: #0078d4;
}

.easyauth-login-button.provider-azure-b2c:hover:not(:disabled) {
  background-color: #0078d4;
  color: white;
}

/* Loading spinner */
.loading-indicator {
  display: inline-flex;
  align-items: center;
}

.spinner {
  width: 16px;
  height: 16px;
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