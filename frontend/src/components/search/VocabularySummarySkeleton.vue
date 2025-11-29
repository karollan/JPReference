<template>
  <v-card
    class="pa-4 mb-4"
    outlined
  >
    <v-row>
      <v-col cols="4">
        <v-skeleton-loader type="text" width="60%" />
        <v-skeleton-loader class="mt-1" type="heading" />
        <div class="d-flex gap-1 mt-2">
          <v-skeleton-loader type="chip" width="60px" />
          <v-skeleton-loader type="chip" width="70px" />
        </div>
      </v-col>
      <v-col class="text-left" cols="8">
        <v-skeleton-loader type="text" width="90%" />
        <v-skeleton-loader class="mt-2" type="text" width="85%" />
        <v-skeleton-loader class="mt-2" type="text" width="80%" />
      </v-col>
    </v-row>
  </v-card>
</template>

<script lang="ts" setup>
</script>
