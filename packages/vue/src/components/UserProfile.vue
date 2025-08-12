<template>
  <div v-if="isLoading">
    <slot name="loading">
      <div>Loading profile...</div>
    </slot>
  </div>

  <div v-else-if="!isAuthenticated || !user">
    <slot name="fallback" />
  </div>

  <div v-else :class="profileClass">
    <slot :user="user">
      <!-- Default profile display -->
      <div class="user-profile">
        <img 
          v-if="showAvatar && user.profilePicture"
          :src="user.profilePicture"
          :alt="user.name || 'User avatar'"
          :style="{ width: '40px', height: '40px', borderRadius: '50%' }"
          class="user-avatar"
        />
        
        <div v-if="showName && user.name" class="user-name">
          {{ user.name }}
        </div>
        
        <div v-if="showEmail && user.email" class="user-email">
          {{ user.email }}
        </div>
        
        <div v-if="showRoles && user.roles && user.roles.length > 0" class="user-roles">
          <span 
            v-for="role in user.roles" 
            :key="role" 
            class="user-role"
          >
            {{ role }}
          </span>
        </div>
      </div>
    </slot>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useAuth } from '../composables/useAuth';

interface Props {
  showAvatar?: boolean;
  showEmail?: boolean;
  showName?: boolean;
  showRoles?: boolean;
  class?: string | string[] | Record<string, boolean>;
}

const props = withDefaults(defineProps<Props>(), {
  showAvatar: true,
  showEmail: true,
  showName: true,
  showRoles: false,
  class: '',
});

const auth = useAuth();

const profileClass = computed(() => {
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

// Computed properties from auth state
const { isLoading, isAuthenticated, user } = auth;
</script>

<style scoped>
.user-profile {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.user-avatar {
  align-self: flex-start;
}

.user-name {
  font-weight: 500;
}

.user-email {
  color: #6b7280;
  font-size: 0.875rem;
}

.user-roles {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.user-role {
  background-color: #f3f4f6;
  color: #374151;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 500;
}
</style>