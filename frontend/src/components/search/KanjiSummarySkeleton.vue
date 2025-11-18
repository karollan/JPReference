<template>
    <v-card
        class="pa-4 mb-4"
        outlined
    >
        <div class="d-flex justify-space-between align-start">
            <v-skeleton-loader type="avatar" width="80" height="80"></v-skeleton-loader>
            <v-skeleton-loader type="button" width="60"></v-skeleton-loader>
        </div>
        <v-skeleton-loader type="text" width="70%" class="mt-3"></v-skeleton-loader>
        <v-skeleton-loader type="text" width="60%" class="mt-2"></v-skeleton-loader>
        <div class="d-flex gap-1 mt-2">
            <v-skeleton-loader type="chip" width="50px"></v-skeleton-loader>
            <v-skeleton-loader type="chip" width="50px"></v-skeleton-loader>
        </div>
    </v-card>
</template>

<script lang="ts" setup>
</script>

<style lang="scss" scoped>
</style>
