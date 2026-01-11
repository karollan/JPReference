<template>
  <div class="schema-viewer">
    <v-treeview
      :items="treeItems"
      item-value="id"
      item-title="title"
      item-children="children"
      density="compact"
      open-on-click
      class="schema-tree"
    >
      <template v-slot:title="{ item }">
        <div class="d-flex align-center">
          <span class="font-weight-medium text-primary mr-2">{{ item.title }}</span>
          <span v-if="item.required" class="text-error mr-2">*</span>
          <span class="text-caption text-medium-emphasis">{{ item.subtitle }}</span>
          
          <v-tooltip v-if="item.description" activator="parent" location="top" max-width="300" :persistent="false">
            {{ item.description }}
          </v-tooltip>
        </div>
      </template>

      <template v-slot:append="{ item }">
        <div class="text-caption text-disabled" v-if="item.rawType">
             <v-chip size="x-small" label class="font-weight-bold" :color="getTypeColor(item.rawType)">
                {{ item.rawType }}
             </v-chip>
        </div>
      </template>
    </v-treeview>
    
    <div v-if="!treeItems.length" class="text-body-2 text-medium-emphasis pa-2">
      No schema definition available.
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  schema: any;
  requiredFields?: string[];
  rootSpec?: any;
}>();

interface TreeItem {
  id: string;
  title: string;
  subtitle?: string;
  description?: string;
  required?: boolean;
  rawType?: string;
  children?: TreeItem[];
}

const resolveRef = (schema: any): any => {
  if (schema?.$ref && props.rootSpec) {
    const ref = schema.$ref;
    if (ref.startsWith('#/components/schemas/')) {
      const name = ref.split('/').pop();
      return props.rootSpec.components?.schemas?.[name] || schema;
    }
  }
  return schema;
};

const getTypeColor = (type?: string) => {
    switch(type) {
        case 'string': return 'blue-lighten-2';
        case 'integer': 
        case 'number': return 'orange-lighten-2';
        case 'boolean': return 'purple-lighten-2';
        case 'array': return 'green-lighten-1';
        case 'object': return 'grey-lighten-1';
        default: return 'grey-lighten-2';
    }
};

const parseSchemaToTree = (schema: any, key: string, path: string, required: boolean): TreeItem => {
  const resolved = resolveRef(schema);
  const type = resolved.type || 'object';
  const format = resolved.format ? `($${resolved.format})` : '';
  const description = resolved.description;
  const isArray = type === 'array';
  const isObject = type === 'object' || !!resolved.properties;
  
  const item: TreeItem = {
    id: path,
    title: key,
    subtitle: format, // Show format in subtitle, type in chip
    description,
    required,
    rawType: type,
  };

  if (isObject && resolved.properties) {
    item.children = Object.entries(resolved.properties).map(([propName, propSchema]: [string, any]) => {
      const isPropRequired = resolved.required?.includes(propName);
      return parseSchemaToTree(propSchema, propName, `${path}.${propName}`, isPropRequired);
    });
  } else if (isArray && resolved.items) {
    // For arrays, show "Array of" + item structure. 
    // We can either make the children the items' properties (if object) or a single child (if primitive)
    const itemsSchema = resolveRef(resolved.items);
    
    // If it's an array of objects, flatten the "Array of" node if preferred, 
    // OR create a dedicated node index. Let's create a dedicated node.
    item.children = [
        parseSchemaToTree(itemsSchema, 'items', `${path}.items`, false)
    ];
  }

  return item;
};

const treeItems = computed(() => {
    if (!props.schema) return [];
    // Start parsing from the root. 
    // We treat the top-level schema as a list of properties if it's an object,
    // or a single root node if preferred.
    // The previous viewer rendered properties directly.
    // To match that, if the root is an object, we return its properties as the top-level tree items.
    
    const root = resolveRef(props.schema);
    
    if ((root.type === 'object' || !root.type) && root.properties) {
        return Object.entries(root.properties).map(([key, val]: [string, any]) => {
             const isReq = props.requiredFields?.includes(key) || root.required?.includes(key);
             return parseSchemaToTree(val, key, `root.${key}`, !!isReq);
        });
    }
    
    if (root.type === 'array') {
         return [ parseSchemaToTree(root, 'Array', 'root', false) ];
    }

    // Fallback for simple types at root
    return [ parseSchemaToTree(root, 'Root', 'root', false) ];
});
</script>
<style scoped>

</style>
