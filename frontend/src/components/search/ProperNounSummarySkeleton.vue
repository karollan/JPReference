<template>
    <v-card
        class="pa-4 mb-4"
        outlined
    >
        <div class="d-flex justify-space-between align-start mb-3">
            <div class="flex-grow-1">
                <v-skeleton-loader type="text" width="40%"></v-skeleton-loader>
                <v-skeleton-loader type="heading" class="mt-1"></v-skeleton-loader>
            </div>
            <v-skeleton-loader type="button" width="60"></v-skeleton-loader>
        </div>
        <v-skeleton-loader type="text" width="85%"></v-skeleton-loader>
        <v-skeleton-loader type="text" width="75%" class="mt-2"></v-skeleton-loader>
    </v-card>
</template>

<script lang="ts" setup>
</script>

<style lang="scss" scoped>
</style>
