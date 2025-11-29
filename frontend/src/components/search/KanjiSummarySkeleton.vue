<template>
  <v-card
    class="pa-4 mb-4"
    outlined
  >
    <div class="d-flex justify-space-between align-start">
      <v-skeleton-loader height="80" type="avatar" width="80" />
      <v-skeleton-loader type="button" width="60" />
    </div>
    <v-skeleton-loader class="mt-3" type="text" width="70%" />
    <v-skeleton-loader class="mt-2" type="text" width="60%" />
    <div class="d-flex gap-1 mt-2">
      <v-skeleton-loader type="chip" width="50px" />
      <v-skeleton-loader type="chip" width="50px" />
    </div>
  </v-card>
</template>

<script lang="ts" setup>
</script>
