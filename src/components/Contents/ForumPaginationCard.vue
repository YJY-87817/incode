<script setup>
import { computed, ref, watch } from "vue";

const props = defineProps({
  currentPage: {
    type: Number,
    required: true,
  },
  totalPages: {
    type: Number,
    required: true,
  },
  pages: {
    type: Array,
    default: () => [],
  },
  showEdgeButtons: {
    type: Boolean,
    default: false,
  },
  showJump: {
    type: Boolean,
    default: false,
  },
  useCaretIcons: {
    type: Boolean,
    default: false,
  },
  paginationClass: {
    type: String,
    default:
      "pagination pagination-sm mb-0 justify-content-center flex-wrap gap-1",
  },
  linkPaddingClass: {
    type: String,
    default: "",
  },
});

const emit = defineEmits(["page-change"]);

const normalizedTotalPages = computed(() =>
  Math.max(Number(props.totalPages || 1), 1),
);

const displayPages = computed(() => {
  if (Array.isArray(props.pages) && props.pages.length > 0) {
    return props.pages;
  }
  return Array.from(
    { length: normalizedTotalPages.value },
    (_, index) => index + 1,
  );
});

const pageInput = ref(String(props.currentPage || 1));

watch(
  () => props.currentPage,
  (currentPage) => {
    pageInput.value = String(currentPage || 1);
  },
  { immediate: true },
);

const clampPage = (value) => {
  const parsedPage = Number.parseInt(value, 10);
  if (!Number.isFinite(parsedPage)) {
    return Math.min(
      Math.max(Number(props.currentPage || 1), 1),
      normalizedTotalPages.value,
    );
  }
  return Math.min(Math.max(parsedPage, 1), normalizedTotalPages.value);
};

const changePage = (page) => {
  const targetPage = clampPage(page);
  emit("page-change", targetPage);
  if (props.showJump) {
    pageInput.value = String(targetPage);
  }
};

const prevPage = () => {
  if (props.currentPage > 1) {
    changePage(props.currentPage - 1);
  }
};

const nextPage = () => {
  if (props.currentPage < normalizedTotalPages.value) {
    changePage(props.currentPage + 1);
  }
};

const jumpToPageInput = () => {
  const targetPage = clampPage(pageInput.value);
  changePage(targetPage);
};
</script>

<template>
  <nav class="d-flex justify-content-center align-items-center flex-wrap gap-2">
    <ul :class="paginationClass">
      <li
        v-if="showEdgeButtons"
        class="page-item"
        :class="{ disabled: currentPage === 1 }"
      >
        <a
          class="page-link edge-page-link"
          href="#"
          aria-label="最前頁"
          @click.prevent="changePage(1)"
        >
          &#8676;
        </a>
      </li>

      <li class="page-item" :class="{ disabled: currentPage === 1 }">
        <a
          class="page-link"
          :class="linkPaddingClass"
          href="#"
          @click.prevent="prevPage"
        >
          <i v-if="useCaretIcons" class="fa-solid fa-caret-left"></i>
          <template v-else>&lsaquo;</template>
        </a>
      </li>

      <li
        v-for="page in displayPages"
        :key="page"
        class="page-item"
        :class="{ active: currentPage === page }"
      >
        <a
          class="page-link"
          :class="linkPaddingClass"
          href="#"
          @click.prevent="changePage(page)"
        >
          {{ page }}
        </a>
      </li>

      <li
        class="page-item"
        :class="{ disabled: currentPage === normalizedTotalPages }"
      >
        <a
          class="page-link"
          :class="linkPaddingClass"
          href="#"
          @click.prevent="nextPage"
        >
          <i v-if="useCaretIcons" class="fa-solid fa-caret-right"></i>
          <template v-else>&rsaquo;</template>
        </a>
      </li>

      <li
        v-if="showEdgeButtons"
        class="page-item"
        :class="{ disabled: currentPage === normalizedTotalPages }"
      >
        <a
          class="page-link edge-page-link"
          href="#"
          aria-label="最後頁"
          @click.prevent="changePage(normalizedTotalPages)"
        >
          &#8677;
        </a>
      </li>
    </ul>

    <div
      v-if="showJump"
      class="d-flex align-items-center gap-2 pagination-jump-wrap"
    >
      <span class="small text-muted">頁碼</span>
      <input
        v-model="pageInput"
        type="number"
        min="1"
        :max="normalizedTotalPages"
        class="form-control form-control-sm pagination-jump-input"
        @keyup.enter="jumpToPageInput"
      />
      <span class="small text-muted pagination-total-pages">
        / {{ normalizedTotalPages }}
      </span>
      <button
        class="btn btn-sm btn-outline-secondary mb-0"
        type="button"
        @click="jumpToPageInput"
      >
        前往
      </button>
    </div>
  </nav>
</template>

<style scoped>
.pagination .page-item.active .page-link {
  background-color: #145369;
  border-color: #145369;
  color: #fff0dd;
}

.pagination .page-link {
  color: #145369;
  border-color: #d1d3d4;
}

.pagination .page-link:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #145369;
}

.pagination .edge-page-link {
  font-size: 1rem;
  font-weight: 400;
  line-height: 1;
}

.pagination-jump-input {
  width: 88px;
}
</style>
