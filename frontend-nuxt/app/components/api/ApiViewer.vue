<template>
  <div class="api-viewer">
    <!-- Loading State -->
    <div v-if="pending" class="d-flex justify-center align-center py-12">
      <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="d-flex flex-column align-center justify-center py-12 text-center">
      <v-icon icon="mdi-server-off" size="64" color="error" class="mb-4"></v-icon>
      <h3 class="text-h5 font-weight-bold mb-2">Service Unavailable</h3>
      <p class="text-body-1 text-medium-emphasis mb-6" style="max-width: 500px">
        Unable to load API documentation. The backend service might be down or unreachable.
      </p>
      <v-btn color="primary" variant="flat" prepend-icon="mdi-refresh" @click="fetchAgain">
        Retry Connection
      </v-btn>
    </div>

    <!-- Content -->
    <div v-else-if="spec" class="api-content">
      <div class="mb-8">
        <h1 class="text-h3 font-weight-bold mb-2">{{ spec.info.title }}</h1>
        <div class="text-subtitle-1 text-medium-emphasis">
          <span class="mr-4">Version {{ spec.info.version }}</span>
          <a v-if="spec.info.contact?.email" :href="`mailto:${spec.info.contact.email}`" class="text-decoration-none">
            {{ spec.info.contact.email }}
          </a>
        </div>
        <p v-if="spec.info.description" class="mt-4 text-body-1">{{ spec.info.description }}</p>
      </div>

      <v-text-field
        v-model="search"
        prepend-inner-icon="mdi-magnify"
        label="Filter endpoints..."
        variant="outlined"
        density="comfortable"
        class="mb-8"
        hide-details
        clearable
      ></v-text-field>

      <EndpointGroup
        v-for="group in filteredGroups"
        :key="group.tag"
        :tag="group.tag"
        :endpoints="group.endpoints"
        :root-spec="spec"
      />
      
      <div v-if="filteredGroups.length === 0" class="text-center py-8 text-medium-emphasis">
        No endpoints match your search.
      </div>

      <div v-if="spec.components?.schemas" class="mt-8">
        <SchemasList :schemas="spec.components.schemas" :root-spec="spec" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import EndpointGroup from './EndpointGroup.vue';
import SchemasList from './SchemasList.vue';
import { useApiService } from '~/services';

const search = ref('');

// Computed to group endpoints by tags
const groups = computed(() => {
  if (!spec.value) return [];
  
  const groupsMap = new Map<string, any[]>();
  
  Object.entries(spec.value.paths).forEach(([path, methods]: [string, any]) => {
    Object.entries(methods).forEach(([method, operation]: [string, any]) => {
      // Skip non-http methods if any (e.g. parameters at path level)
      if (['get', 'post', 'put', 'delete', 'patch', 'options', 'head'].includes(method)) {
        const tag = operation.tags?.[0] || 'Default';
        
        if (!groupsMap.has(tag)) {
          groupsMap.set(tag, []);
        }
        
        groupsMap.get(tag)?.push({
          method: method.toUpperCase(),
          path,
          operation
        });
      }
    });
  });

  return Array.from(groupsMap.entries())
    .map(([tag, endpoints]) => ({ tag, endpoints }))
    .sort((a, b) => a.tag.localeCompare(b.tag));
});

const filteredGroups = computed(() => {
  if (!search.value) return groups.value;
  
  const query = search.value.toLowerCase();
  
  return groups.value.map(group => ({
    tag: group.tag,
    endpoints: group.endpoints.filter((ep: any) => 
      ep.path.toLowerCase().includes(query) || 
      ep.operation.summary?.toLowerCase().includes(query) ||
      group.tag.toLowerCase().includes(query)
    )
  })).filter(group => group.endpoints.length > 0);
});

const { data: spec, pending, error } = await useAsyncData(
  'spec',
  () => useApiService().getApiSpec(),
  {
    server: true
  }
)

const fetchAgain = () => {
  refreshNuxtData('spec')
}
</script>
