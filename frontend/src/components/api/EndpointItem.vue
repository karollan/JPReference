<template>
  <v-card variant="outlined" class="endpoint-item mb-4" :class="methodClass">
    <v-expansion-panels>
      <v-expansion-panel elevation="0" bg-color="transparent">
        <v-expansion-panel-title class="endpoint-header py-2">
          <div class="d-flex align-center flex-wrap" style="width: 100%">
            <v-chip
              :color="methodColor"
              size="small"
              class="mr-3 font-weight-bold text-uppercase"
              label
            >
              {{ method }}
            </v-chip>
            <span class="text-subtitle-1 font-weight-medium font-mono mr-4">{{ path }}</span>
            <span class="text-body-2 text-medium-emphasis text-truncate flex-grow-1">
              {{ summary }}
            </span>
          </div>
        </v-expansion-panel-title>

        <v-expansion-panel-text>
          <div class="pt-2">
            <div v-if="description" class="text-body-2 mb-4">{{ description }}</div>

            <!-- Parameters -->
            <div v-if="parameters && parameters.length > 0" class="mb-4">
              <div class="d-flex justify-space-between align-center mb-2">
                <div class="text-subtitle-2">Parameters</div>
                <v-btn
                  v-if="!tryMode"
                  size="small"
                  variant="text"
                  color="primary"
                  prepend-icon="mdi-play"
                  @click="tryMode = true"
                >
                  Try it out
                </v-btn>
                <v-btn
                  v-else
                  size="small"
                  variant="text"
                  color="error"
                  prepend-icon="mdi-close"
                  @click="cancelTryMode"
                >
                  Cancel
                </v-btn>
              </div>

              <v-table density="compact" class="parameters-table">
                <thead>
                  <tr>
                    <th style="width: 200px">Name</th>
                    <th>In</th>
                    <th>Description</th>
                    <th v-if="tryMode">Value</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="param in parameters" :key="param.name">
                    <td class="font-weight-medium">
                      {{ param.name }}
                      <span v-if="param.required" class="text-error">*</span>
                    </td>
                    <td>{{ param.in }}</td>
                    <td class="text-caption">
                      <div>{{ param.description }}</div>
                      <div class="text-medium-emphasis font-mono mt-1">
                        {{ formatParamType(param) }}
                      </div>
                    </td>
                    <td v-if="tryMode" class="py-2">
                      <v-checkbox
                        v-if="isBoolean(param)"
                        v-model="paramValues[param.name]"
                        density="compact"
                        hide-details
                      ></v-checkbox>
                      <v-text-field
                        v-else
                        v-model="paramValues[param.name]"
                        density="compact"
                        variant="outlined"
                        hide-details
                        :placeholder="formatParamType(param)"
                      ></v-text-field>
                    </td>
                  </tr>
                </tbody>
              </v-table>
            </div>

            <div v-else-if="!parameters?.length && !tryMode" class="d-flex justify-end mb-4">
               <v-btn
                  size="small"
                  variant="text"
                  color="primary"
                  prepend-icon="mdi-play"
                  @click="tryMode = true"
                >
                  Try it out
                </v-btn>
            </div>

            <!-- Request Body -->
            <div v-if="requestBody" class="mb-4">
              <div class="text-subtitle-2 mb-2">Request Body</div>
              <div class="pa-3 bg-surface rounded">
                <div v-if="tryMode">
                  <v-textarea
                    v-model="requestBodyValue"
                    variant="outlined"
                    rows="8"
                    hide-details
                    class="font-mono text-body-2"
                  ></v-textarea>
                </div>
                <SchemaViewer 
                  v-else
                  v-if="requestBodyContent.schema"
                  :schema="requestBodyContent.schema"
                  :root-spec="rootSpec" 
                />
              </div>
            </div>

            <!-- Execute Button -->
            <div v-if="tryMode" class="mb-6">
              <v-btn
                color="primary"
                :loading="executing"
                prepend-icon="mdi-lightning-bolt"
                @click="executeRequest"
                block
              >
                Execute
              </v-btn>
            </div>

            <!-- Execution Result -->
            <div v-if="executionResult" class="mb-6">
              <div class="text-subtitle-2 mb-2">Server Response</div>
              <v-card variant="tonal" :color="responseColor">
                 <v-card-title class="text-subtitle-1 font-weight-bold d-flex align-center">
                    <span>Code: {{ executionResult.status }}</span>
                    <v-spacer></v-spacer>
                    <span class="text-caption">{{ executionResult.duration }}ms</span>
                 </v-card-title>
                 <v-divider></v-divider>
                 <v-card-text>
                    <div class="font-mono text-caption mb-2">Response Body</div>
                    <pre class="overflow-auto" style="max-height: 300px">{{ executionResult.body }}</pre>
                 </v-card-text>
              </v-card>
            </div>

            <!-- Responses Documentation -->
            <div>
              <div class="text-subtitle-2 mb-2">Responses</div>
              <v-expansion-panels variant="accordion">
                <v-expansion-panel
                  v-for="(response, code) in responses"
                  :key="code"
                  :title="`${code} ${response.description || ''}`"
                >
                  <v-expansion-panel-text>
                    <SchemaViewer 
                      v-if="getResponseSchema(response)"
                      :schema="getResponseSchema(response)"
                      :root-spec="rootSpec" 
                    />
                    <div v-else class="text-caption text-disabled">No content</div>
                  </v-expansion-panel-text>
                </v-expansion-panel>
              </v-expansion-panels>
            </div>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>
  </v-card>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import SchemaViewer from './SchemaViewer.vue';

const props = defineProps<{
  method: string;
  path: string;
  operation: any;
  rootSpec?: any;
}>();

const summary = computed(() => props.operation.summary);
const description = computed(() => props.operation.description);
const parameters = computed(() => props.operation.parameters || []);
const responses = computed(() => props.operation.responses || {});
const requestBody = computed(() => props.operation.requestBody);

const requestBodyContent = computed(() => {
  if (!requestBody.value?.content) return null;
  return requestBody.value.content['application/json'] || 
         requestBody.value.content['text/json'] ||
         Object.values(requestBody.value.content)[0];
});

const getResponseSchema = (response: any) => {
  if (!response.content) return null;
  // Handle different content types more robustly
  const content = response.content['application/json'] || 
                  response.content['text/json'] ||
                  Object.values(response.content)[0];
  return content?.schema;
};

// --- Styling ---
const methodColor = computed(() => {
  const map: Record<string, string> = {
    get: 'blue',
    post: 'green',
    put: 'orange',
    delete: 'red',
    patch: 'purple'
  };
  return map[props.method.toLowerCase()] || 'grey';
});
const methodClass = computed(() => `method-${props.method.toLowerCase()}`);
const formatParamType = (param: any) => {
  if (param.schema) {
    return param.schema.type + (param.schema.format ? ` (${param.schema.format})` : '');
  }
  return 'any';
};
const isBoolean = (param: any) => param.schema?.type === 'boolean';

// --- Interactive Mode ---
const tryMode = ref(false);
const paramValues = ref<Record<string, any>>({});
const requestBodyValue = ref('');
const executing = ref(false);
const executionResult = ref<any>(null);

const responseColor = computed(() => {
  if (!executionResult.value) return 'grey';
  const status = executionResult.value.status;
  if (status >= 200 && status < 300) return 'success';
  if (status >= 400) return 'error';
  return 'warning';
});

// Initialize params when entering try mode
watch(tryMode, (newValue) => {
  if (newValue) {
    parameters.value.forEach((p: any) => {
      // Set default value if available, else empty
      /* eslint-disable-next-line */ 
      if (paramValues.value[p.name] === undefined) {
         if (p.schema?.default !== undefined) paramValues.value[p.name] = p.schema.default;
         else if (isBoolean(p)) paramValues.value[p.name] = false;
         else paramValues.value[p.name] = '';
      }
    });

    if (requestBodyContent.value && !requestBodyValue.value) {
       // Try to generate a sample body from schema (very basic)
       requestBodyValue.value = "{\n  \n}";
    }
  }
});

const cancelTryMode = () => {
    tryMode.value = false;
    executionResult.value = null;
};

const executeRequest = async () => {
  executing.value = true;
  executionResult.value = null;
  
  const startTime = performance.now();
  
  try {
    // 1. Construct URL
    let apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000';
    apiUrl = apiUrl.replace(/\/api\/?$/, '');
    
    // Replace Path Parameters
    let path = props.path;
    const queryParams = new URLSearchParams();
    
    parameters.value.forEach((p: any) => {
      const val = paramValues.value[p.name];
      if (p.in === 'path') {
        path = path.replace(`{${p.name}}`, encodeURIComponent(String(val)));
      } else if (p.in === 'query' && val !== '' && val !== null && val !== undefined) {
          // Handle array parameters
          if (p.schema?.type === 'array' && typeof val === 'string' && val.includes(',')) {
              val.split(',').forEach((v: string) => queryParams.append(p.name, v.trim()));
          } else {
              queryParams.append(p.name, String(val));
          }
      }
    });
    
    let fullUrl = `${apiUrl}${path}`;
    if (queryParams.toString()) {
      fullUrl += `?${queryParams.toString()}`;
    }

    // 2. Prepare Options
    const options: RequestInit = {
      method: props.method.toUpperCase(),
      headers: {
        'Content-Type': 'application/json',
      },
    };

    if (props.method.toLowerCase() !== 'get' && props.method.toLowerCase() !== 'head') {
      if (requestBodyValue.value) {
        options.body = requestBodyValue.value;
      }
    }

    // 3. Fetch
    const response = await fetch(fullUrl, options);
    const duration = Math.round(performance.now() - startTime);
    
    let body = '';
    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
      const json = await response.json();
      body = JSON.stringify(json, null, 2);
    } else {
      body = await response.text();
    }

    executionResult.value = {
      status: response.status,
      body,
      duration
    };

  } catch (e: any) {
    executionResult.value = {
      status: 0,
      body: e.message || 'Network Error',
      duration: Math.round(performance.now() - startTime)
    };
  } finally {
    executing.value = false;
  }
};
</script>

<style scoped>
.font-mono {
  font-family: monospace;
}
.method-get { border-left: 4px solid rgb(var(--v-theme-info)); }
.method-post { border-left: 4px solid rgb(var(--v-theme-success)); }
.method-put { border-left: 4px solid rgb(var(--v-theme-warning)); }
.method-delete { border-left: 4px solid rgb(var(--v-theme-error)); }
.method-patch { border-left: 4px solid #9c27b0; }
</style>
