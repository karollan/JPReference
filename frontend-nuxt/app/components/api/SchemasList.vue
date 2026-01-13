<template>
  <div class="schemas-list">
    <div class="d-flex align-center mb-4">
      <h2 class="text-h5 font-weight-bold">Schemas</h2>
      <v-divider class="ml-4"></v-divider>
    </div>

    <v-expansion-panels multiple>
      <v-expansion-panel
        v-for="(schema, name) in schemas"
        :key="name"
        :title="name"
      >
        <v-expansion-panel-text>
          <div class="pa-2">
            <div v-if="schema.description" class="text-body-2 mb-3 text-medium-emphasis">
              {{ schema.description }}
            </div>
            <SchemaViewer :schema="schema" :root-spec="rootSpec" />
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </div>
</template>

<script setup lang="ts">
import SchemaViewer from './SchemaViewer.vue';

defineProps<{
  schemas: Record<string, any>;
  rootSpec: any;
}>();
</script>
