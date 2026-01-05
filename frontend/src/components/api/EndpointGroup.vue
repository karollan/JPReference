<template>
  <div class="endpoint-group mb-6">
    <div class="d-flex align-center mb-4">
      <h2 class="text-h5 font-weight-bold">{{ tag }}</h2>
      <v-divider class="ml-4"></v-divider>
    </div>
    
    <div class="endpoints-list">
      <EndpointItem
        v-for="endpoint in endpoints"
        :key="`${endpoint.method}-${endpoint.path}`"
        v-bind="endpoint"
        :root-spec="rootSpec"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import EndpointItem from './EndpointItem.vue';

defineProps<{
  tag: string;
  endpoints: Array<{
    method: string;
    path: string;
    operation: any;
  }>;
  rootSpec: any;
}>();
</script>
