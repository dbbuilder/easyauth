<template>
  <div v-if="isLoading">
    <slot name="loading">
      <div>Loading...</div>
    </slot>
  </div>

  <div v-else-if="!isAuthenticated">
    <slot name="fallback">
      <div>Authentication required</div>
    </slot>
  </div>

  <div v-else-if="!hasRequiredRoles">
    <slot name="unauthorized">
      <div>Insufficient permissions</div>
    </slot>
  </div>

  <div v-else>
    <slot />
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useAuth } from '../composables/useAuth';

interface Props {
  requiredRoles?: string[];
  requireAllRoles?: boolean;
  onUnauthorized?: () => void;
  redirectTo?: string;
}

const props = withDefaults(defineProps<Props>(), {
  requiredRoles: () => [],
  requireAllRoles: false,
});

const auth = useAuth();
const { isAuthenticated, isLoading, user } = auth;

const hasRequiredRoles = computed(() => {
  if (props.requiredRoles.length === 0 || !user.value) {
    return true;
  }
  
  const userRoles = (user.value as any)?.roles || [];
  
  return props.requireAllRoles
    ? props.requiredRoles.every(role => userRoles.includes(role))
    : props.requiredRoles.some(role => userRoles.includes(role));
});

onMounted(() => {
  if (!isLoading.value && !isAuthenticated.value) {
    if (props.redirectTo) {
      window.location.href = props.redirectTo;
      return;
    }
    
    props.onUnauthorized?.();
  }
  
  if (!isLoading.value && isAuthenticated.value && !hasRequiredRoles.value) {
    props.onUnauthorized?.();
  }
});
</script>