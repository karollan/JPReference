<template>
  <v-card
    class="pa-4 mb-4"
    outlined
  >
    <div class="d-flex justify-space-between align-start mb-3">
      <div class="flex-grow-1">
        <v-skeleton-loader type="text" width="40%" />
        <v-skeleton-loader class="mt-1" type="heading" />
      </div>
      <v-skeleton-loader type="button" width="60" />
    </div>
    <v-skeleton-loader type="text" width="85%" />
    <v-skeleton-loader class="mt-2" type="text" width="75%" />
  </v-card>
</template>

<script lang="ts" setup>
</script>
