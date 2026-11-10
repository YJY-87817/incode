<!-- TopicList.vue -->
<!--
  主題列表視圖：載入中 / 空狀態、手機卡片列表、桌機表格列表、排序下拉與表頭分頁
  由 TopicListView.vue 在沒有選取主題時顯示；版面（手機 / 桌機）由外層卡片寬度決定（見 useTopicListLayout.js）
-->

<script setup>
import { computed, inject } from "vue";
import InteractionButtons from "@/components/contents/InteractionButtons.vue";
import ForumPaginationCard from "@/components/contents/ForumPaginationCard.vue";
import HtmlIframe from "@/components/common/HtmlIframe.vue";
import UserPreview from "@/components/users/UserPreview.vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore.js";
import { useForumNavigation } from "@/composables/useForumNavigation.js";
import { useTopicInteractionHandlers } from "@/composables/useTopicInteractionHandlers.js";
import { TOPIC_LIST_LAYOUT_KEY } from "@/composables/useTopicListLayout.js";
import { useTopicSort } from "@/composables/useTopicSort.js";
import { formatDate } from "@/utils/contentBody.js";
import { getAvatarUrl } from "@/utils/avatar.js";
import { buildDisplayPages } from "@/utils/pagination.js";

const forumStore = useForumStore();
const { goToThread, goToPage } = useForumNavigation();
const {
  handleTopicInteractionSuccess,
  handleTopicInteractionError,
  handleTopicEditContent,
  handleTopicDeleteContent,
} = useTopicInteractionHandlers();

// 版面狀態由外層卡片量測（見 TopicListView.vue / useTopicListLayout.js）
const {
  showMobileTopicList,
  isCompactDesktopHeaderPagination,
  isDenseDesktopHeaderPagination,
} = inject(TOPIC_LIST_LAYOUT_KEY);

const {
  sortOption,
  sortOptions,
  showSortDropdown,
  desktopSortDropdownRef,
  mobileSortDropdownRef,
  shouldHideSortIcon,
  isSortControlCompact,
  sortInputLabel,
  sortControlWidth,
  selectSortOption,
  toggleSortDropdown,
} = useTopicSort();

const basePreviewCss = `
  body {
    margin: 0;
    padding: 0;
    white-space: normal;
    line-height: 1.3;
  }
  * {
    margin: 0;
    padding: 0;
  }
  p, div, br {
    margin: 0;
    padding: 0;
    line-height: 1.3;
  }
  br {
    display: none;
  }
  img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 2px 0;
  }
  a {
    color: inherit;
    text-decoration: none;
  }
`;

const getTopicPreviewCss = (topic) => {
  const hasImages = Array.isArray(topic?.imageUrls) && topic.imageUrls.length;
  if (hasImages) return `${basePreviewCss} img { display: none; }`;
  return basePreviewCss;
};

const mobileDisplayPages = computed(() =>
  buildDisplayPages(
    forumStore.pagination.currentPage,
    forumStore.pagination.totalPages,
    3,
  ),
);

const desktopHeaderPages = computed(() =>
  buildDisplayPages(
    forumStore.pagination.currentPage,
    forumStore.pagination.totalPages,
    isDenseDesktopHeaderPagination.value ? 3 : 5,
  ),
);

const desktopHeaderPaginationClass = computed(() =>
  isDenseDesktopHeaderPagination.value
    ? "pagination pagination-sm mb-0 justify-content-center gap-0 topic-header-pagination-list"
    : undefined,
);

const desktopHeaderLinkPaddingClass = computed(() =>
  isDenseDesktopHeaderPagination.value
    ? "topic-header-pagination-link"
    : undefined,
);
</script>

<template>
  <div>
    <!-- 載入中 -->
    <div
      v-if="forumStore.isLoading && forumStore.topics.length === 0"
      class="text-center py-5"
    >
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">載入中...</span>
      </div>
    </div>

    <!-- 空狀態 -->
    <div
      v-else-if="forumStore.topics.length === 0"
      class="text-center py-5 text-muted"
    >
      <p>此分類目前沒有主題</p>
    </div>

    <div
      v-if="forumStore.topics.length > 0 && showMobileTopicList"
      class="px-2 pt-2 pb-1"
    >
      <div class="topic-mobile-toolbar">
        <div
          ref="mobileSortDropdownRef"
          class="topic-sort-dropdown topic-mobile-sort-dropdown"
          :style="{ '--sort-control-width': sortControlWidth }"
        >
          <div
            class="topic-sort-input-wrapper"
            @click.stop="toggleSortDropdown"
          >
            <input
              :value="sortInputLabel"
              class="form-control topic-sort-input"
              :class="{
                'topic-sort-input--expanded': isSortControlCompact,
              }"
              readonly
              aria-label="主題排序"
              @click.stop="toggleSortDropdown"
            />
            <span
              v-if="!shouldHideSortIcon"
              class="topic-sort-input-dropdown"
              aria-hidden="true"
              @click.stop="toggleSortDropdown"
            >
              ▾
            </span>
          </div>
          <div v-if="showSortDropdown" class="topic-sort-menu">
            <button
              v-for="option in sortOptions"
              :key="option.value"
              type="button"
              class="topic-sort-item"
              :class="{
                active: option.value === sortOption.value,
              }"
              @mousedown.prevent="selectSortOption(option)"
            >
              {{ option.label }}
            </button>
          </div>
        </div>

        <div
          v-if="forumStore.pagination.totalPages > 1"
          class="topic-mobile-pagination"
        >
          <ForumPaginationCard
            :current-page="forumStore.pagination.currentPage"
            :total-pages="forumStore.pagination.totalPages"
            :pages="mobileDisplayPages"
            :show-edge-buttons="false"
            :show-jump="true"
            pagination-class="pagination pagination-sm mb-0 justify-content-center gap-0 topic-header-pagination-list"
            link-padding-class="topic-header-pagination-link"
            @page-change="goToPage"
          />
        </div>
      </div>
    </div>

    <!-- 手機卡片列表 -->
    <div
      v-if="forumStore.topics.length > 0 && showMobileTopicList"
      class="px-2 pb-2"
    >
      <div
        v-for="topic in forumStore.topics"
        :key="`mobile-topic-${topic.id}`"
        class="card topic-mobile-card mb-2"
        @click="goToThread(topic.id)"
      >
        <div class="card-body pt-2 pb-0 px-2">
          <div
            class="d-flex align-items-center justify-content-between gap-2 mb-1 flex-wrap"
          >
            <div class="d-flex flex-wrap gap-1 align-items-center">
              <span
                v-if="topic.categoryName"
                class="badge bg-secondary-light text-dark interaction-aligned-badge"
              >
                {{ topic.categoryName }}
              </span>
              <span
                v-if="topic.hasEntrustMission"
                class="badge bg-secondary interaction-aligned-badge"
                >委託任務</span
              >
              <span
                v-if="topic.isHot"
                class="badge bg-warning text-dark interaction-aligned-badge"
                >熱門</span
              >
              <div class="interaction-container" @click.stop>
                <InteractionButtons
                  :key="`topic-mobile-interactions-${topic.id}`"
                  :contentId="topic.id"
                  :authorUserId="topic.userId"
                  @interaction-success="handleTopicInteractionSuccess"
                  @interaction-error="handleTopicInteractionError"
                  @edit-content="handleTopicEditContent"
                  @delete-content="handleTopicDeleteContent"
                />
              </div>
            </div>
          </div>

          <div
            class="d-flex align-items-center justify-content-between gap-2 flex-wrap mt-1"
          >
            <h6 class="mb-0 fs-6 fw-bold flex-grow-1">
              <a
                href="javascript:void(0)"
                class="topic-title-link text-break"
              >
                {{ topic.title }}
              </a>
            </h6>
          </div>

          <div class="topic-preview-body topic-preview-collapsed mt-2 mb-2">
            <div
              v-if="topic.imageUrls && topic.imageUrls.length"
              class="topic-preview-images-col"
            >
              <div class="topic-preview-images-grid">
                <img
                  v-for="(url, idx) in topic.imageUrls.slice(0, 1)"
                  :key="url + idx"
                  :src="url"
                  alt="貼文圖片"
                  class="topic-preview-image-thumb"
                  loading="lazy"
                />
              </div>
            </div>
            <div class="topic-preview-text-col">
              <HtmlIframe
                class="topic-preview-iframe"
                :html="topic.contentHtml || topic.content"
                :min-height="0"
                text-color="#145369"
                :extra-css="getTopicPreviewCss(topic)"
              />
            </div>
            <div
              class="topic-preview-click-layer"
              @click="goToThread(topic.id)"
            ></div>
          </div>

          <div
            class="d-flex justify-content-between align-items-center small text-muted"
          >
            <UserPreview
              :user-id="topic.userId"
              :username="topic.author"
              :avatar-url="getAvatarUrl(topic.author, topic.authorAvatar)"
              :size="22"
            />
            <span>{{ formatDate(topic.createdAt) }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 桌機表格列表 -->
    <div
      v-if="forumStore.topics.length > 0 && !showMobileTopicList"
      class="table-responsive"
    >
      <table class="table table-hover mb-0 topic-list-table">
        <thead class="topic-table-header">
          <tr>
            <th class="px-3 py-2 py-md-3 topic-col">
              <div
                class="d-flex align-items-center flex-nowrap gap-2 gap-lg-4 w-100 topic-header-main-row"
              >
                <span class="topic-table-header-label"> 📝 主題 </span>
                <div
                  ref="desktopSortDropdownRef"
                  class="topic-sort-dropdown"
                  :style="{ '--sort-control-width': sortControlWidth }"
                >
                  <div
                    class="topic-sort-input-wrapper"
                    @click.stop="toggleSortDropdown"
                  >
                    <input
                      :value="sortInputLabel"
                      class="form-control topic-sort-input"
                      :class="{
                        'topic-sort-input--expanded': isSortControlCompact,
                      }"
                      readonly
                      aria-label="主題排序"
                      @click.stop="toggleSortDropdown"
                    />
                    <span
                      v-if="!shouldHideSortIcon"
                      class="topic-sort-input-dropdown"
                      aria-hidden="true"
                      @click.stop="toggleSortDropdown"
                    >
                      ▾
                    </span>
                  </div>
                  <div v-if="showSortDropdown" class="topic-sort-menu">
                    <button
                      v-for="option in sortOptions"
                      :key="option.value"
                      type="button"
                      class="topic-sort-item"
                      :class="{
                        active: option.value === sortOption.value,
                      }"
                      @mousedown.prevent="selectSortOption(option)"
                    >
                      {{ option.label }}
                    </button>
                  </div>
                </div>

                <div
                  v-if="forumStore.pagination.totalPages > 1"
                  class="ms-auto topic-header-pagination"
                >
                  <ForumPaginationCard
                    :current-page="forumStore.pagination.currentPage"
                    :total-pages="forumStore.pagination.totalPages"
                    :pages="desktopHeaderPages"
                    :show-edge-buttons="!isCompactDesktopHeaderPagination"
                    :show-jump="true"
                    :show-direction-buttons="
                      !isCompactDesktopHeaderPagination
                    "
                    :pagination-class="desktopHeaderPaginationClass"
                    :link-padding-class="desktopHeaderLinkPaddingClass"
                    @page-change="goToPage"
                  />
                </div>
              </div>
            </th>
            <th class="d-none d-md-table-cell created-col">
              <span class="topic-table-header-label"> 🗓️ 發佈 </span>
            </th>
            <th class="d-none d-md-table-cell last-reply-col">
              <span class="topic-table-header-label"> 🕒 回應 </span>
            </th>
            <th class="reply-col">
              <span class="topic-table-header-label"> 回應數 </span>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="topic in forumStore.topics"
            :key="topic.id"
            class="forum-row"
            @click="goToThread(topic.id)"
          >
            <!-- 主題欄 -->
            <td class="px-3 pt-2 pb-0 pt-md-2 pb-md-0 topic-col">
              <div class="d-flex align-items-start gap-3">
                <div class="flex-grow-1 min-width-0">
                  <!-- 主題徽章 -->
                  <div class="d-flex flex-wrap gap-1 align-items-center">
                    <span
                      v-if="topic.categoryName"
                      class="badge bg-secondary-light text-dark interaction-aligned-badge"
                    >
                      {{ topic.categoryName }}
                    </span>
                    <button
                      v-if="
                        topic.type === CONTENT_TYPES.REPLY &&
                        topic.parentContentId
                      "
                      class="badge interaction-aligned-badge parent-link-btn"
                      type="button"
                      @click.stop="goToThread(topic.parentContentId)"
                    >
                      <i class="fa-solid fa-people-pulling"></i>
                      主文
                    </button>
                    <span
                      v-if="topic.hasEntrustMission"
                      class="badge bg-secondary interaction-aligned-badge"
                    >
                      委託任務
                    </span>
                    <span
                      v-if="topic.isHot"
                      class="badge bg-warning text-dark interaction-aligned-badge"
                    >
                      熱門
                    </span>
                    <div class="interaction-container ms-auto" @click.stop>
                      <InteractionButtons
                        :key="`topic-interactions-${topic.id}`"
                        :contentId="topic.id"
                        :authorUserId="topic.userId"
                        @interaction-success="handleTopicInteractionSuccess"
                        @interaction-error="handleTopicInteractionError"
                        @edit-content="handleTopicEditContent"
                        @delete-content="handleTopicDeleteContent"
                      />
                    </div>
                  </div>

                  <!-- 主題標題 + 互動按鈕 (同一行) -->
                  <div
                    class="d-flex align-items-center justify-content-between gap-2 flex-wrap mt-1"
                  >
                    <h6 class="mb-0 fs-6 fw-bold">
                      <a
                        href="javascript:void(0)"
                        class="topic-title-link text-break"
                      >
                        {{ topic.title }}
                      </a>
                    </h6>
                  </div>

                  <div
                    class="topic-preview-body topic-preview-collapsed mb-2"
                  >
                    <div
                      v-if="topic.imageUrls && topic.imageUrls.length"
                      class="topic-preview-images-col"
                    >
                      <div class="topic-preview-images-grid">
                        <img
                          v-for="(url, idx) in topic.imageUrls.slice(0, 1)"
                          :key="url + idx"
                          :src="url"
                          alt="貼文圖片"
                          class="topic-preview-image-thumb"
                          loading="lazy"
                        />
                      </div>
                    </div>
                    <div class="topic-preview-text-col">
                      <HtmlIframe
                        class="topic-preview-iframe"
                        :html="topic.contentHtml || topic.content"
                        :min-height="0"
                        text-color="#145369"
                        :extra-css="getTopicPreviewCss(topic)"
                      />
                    </div>
                    <div
                      class="topic-preview-click-layer"
                      @click="goToThread(topic.id)"
                    ></div>
                  </div>

                  <!-- 互動按鈕已移到分類徽章同列 -->
                </div>
              </div>
            </td>

            <!-- 發佈時間欄 -->
            <td class="d-none d-md-table-cell text-muted small created-col">
              <div class="meta-cell-stack">
                <div class="meta-content-group">
                  <div class="meta-date-text">
                    {{ formatDate(topic.createdAt) }}
                  </div>
                  <div class="meta-user-row text-muted">
                    <UserPreview
                      :user-id="topic.userId"
                      :username="topic.author"
                      :avatar-url="
                        getAvatarUrl(topic.author, topic.authorAvatar)
                      "
                      :size="24"
                    />
                  </div>
                </div>
              </div>
            </td>

            <!-- 最後回應時間欄 -->
            <td
              class="d-none d-md-table-cell text-muted small last-reply-col"
            >
              <div class="meta-cell-stack">
                <div class="meta-content-group">
                  <div v-if="topic.lastReplyTime" class="meta-date-text">
                    {{ formatDate(topic.lastReplyTime) }}
                  </div>
                  <div v-else class="meta-date-text text-muted">
                    尚無回應
                  </div>
                  <div
                    v-if="topic.lastReplyUser"
                    class="meta-user-row text-muted"
                  >
                    <UserPreview
                      :user-id="topic.lastReplyUserId"
                      :username="topic.lastReplyUser"
                      :avatar-url="
                        getAvatarUrl(
                          topic.lastReplyUser,
                          topic.lastReplyUserAvatar,
                        )
                      "
                      :size="24"
                    />
                  </div>
                </div>
              </div>
            </td>

            <!-- 回應數欄 -->
            <td class="reply-col">
              <div class="meta-cell-center">
                <div class="reply-count-badge badge bg-primary">
                  {{ topic.replyCount }}
                </div>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
/* 卡片樣式 */
.card {
  border-color: #d1d3d4;
}

.card-body {
  margin-bottom: 0;
  padding-bottom: 0;
}

.topic-preview-body {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  position: relative;
}

.topic-preview-collapsed {
  max-height: 65px;
  overflow: hidden;
  position: relative;
}

.topic-preview-images-col {
  flex: 0 0 auto;
}

.topic-preview-images-grid {
  display: grid;
  grid-template-columns: 56px;
  gap: 8px;
}

.topic-preview-image-thumb {
  width: 56px;
  height: 56px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid #d1d3d4;
  background: #fff0dd;
}

.topic-preview-text-col {
  flex: 1 1 auto;
  min-width: 0;
}

.topic-preview-iframe {
  width: 100%;
  display: flex;
  flex-direction: column;
}

.topic-preview-click-layer {
  position: absolute;
  inset: 0;
  z-index: 2;
}

.topic-table-header {
  background-color: #86b0bd;
  color: #fff;
}

.topic-table-header th {
  color: #fff;
  padding-top: 1rem !important;
  padding-bottom: 1rem !important;
  padding-left: 1rem !important;
  padding-right: 1rem !important;
  border-bottom-color: #d1d3d4;
}

.topic-table-header-label {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  color: #fff;
  font-weight: 700;
  font-size: 1rem;
  line-height: 1.2;
}

.topic-sort-dropdown {
  position: relative;
  display: inline-flex;
  width: var(--sort-control-width, auto);
  max-width: 100%;
  color: #145369;
  transition: width 0.16s ease;
}

.topic-header-pagination {
  display: flex;
  align-items: center;
  min-width: 0;
  flex-shrink: 1;
}

.topic-header-main-row {
  flex-wrap: nowrap !important;
  min-width: 0;
}

.topic-mobile-toolbar {
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  flex-wrap: nowrap;
  gap: 0.6rem;
  padding: 0.75rem;
  border: 1px solid #d1d3d4;
  border-radius: 0.6rem;
  background-color: #86b0bd;
  position: relative;
}

.topic-mobile-sort-dropdown {
  flex: 0 0 auto;
  max-width: 48%;
}

.topic-mobile-pagination {
  display: flex;
  flex: 1 1 auto;
  justify-content: flex-end;
  align-items: center;
  min-width: 0;
  gap: 0.35rem;
}

.topic-mobile-pagination :deep(nav),
.topic-mobile-pagination :deep(.pagination),
.topic-mobile-pagination :deep(.pagination-jump-shell),
.topic-mobile-pagination :deep(.pagination-jump-wrap) {
  flex-wrap: nowrap !important;
}

.topic-mobile-pagination :deep(.pagination-jump-shell) {
  background-color: #fff;
  margin-left: 0;
  padding: 0.08rem 0.3rem;
  border: 1px solid #d1d3d4;
  border-radius: 0.35rem;
  font-size: 0.75rem;
}

.topic-mobile-pagination :deep(.pagination-jump-wrap) {
  gap: 0.25rem !important;
}

.topic-mobile-pagination :deep(.pagination-jump-input) {
  font-size: 0.7rem;
  padding: 0.05rem 0.15rem;
  min-width: calc(1ch + 1em);
  color: #145369;
  border-color: #86b0bd;
  background-color: transparent;
  -moz-appearance: auto;
  appearance: auto;
}

.topic-mobile-pagination
  :deep(.pagination-jump-input::-webkit-outer-spin-button),
.topic-mobile-pagination
  :deep(.pagination-jump-input::-webkit-inner-spin-button) {
  -webkit-appearance: inner-spin-button;
  opacity: 1;
  margin: 0;
}

.topic-mobile-pagination :deep(.pagination-jump-button) {
  padding: 0.05rem 0.2rem;
  font-size: 0.7rem;
  color: #145369;
  border-color: #86b0bd;
  background-color: #fff;
}

.topic-mobile-pagination :deep(.pagination-total-pages) {
  font-size: 0.7rem;
  color: #145369 !important;
}

.topic-mobile-pagination :deep(.topic-header-pagination-list .page-link) {
  padding: 0.12rem 0.3rem;
  font-size: 0.7rem;
  min-width: 1.5rem;
}

.topic-header-pagination :deep(nav),
.topic-header-pagination :deep(.pagination),
.topic-header-pagination :deep(.pagination-jump-shell),
.topic-header-pagination :deep(.pagination-jump-wrap) {
  flex-wrap: nowrap !important;
}

.topic-header-pagination :deep(.topic-header-pagination-list .page-link) {
  padding: 0.15rem 0.35rem;
  font-size: 0.75rem;
  line-height: 1.1;
  min-width: 1.65rem;
}

.topic-header-pagination :deep(.pagination-jump-shell) {
  margin-left: 0.35rem;
  padding: 0.08rem 0.3rem;
  border: 1px solid #d1d3d4;
  border-radius: 0.35rem;
  background-color: #fff;
  font-size: 0.75rem;
}

.topic-header-pagination :deep(.pagination-jump-wrap) {
  gap: 0.35rem !important;
}

.topic-header-pagination :deep(.pagination-jump-input) {
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.05rem 0.2rem;
  min-width: calc(1ch + 1.3em);
  color: #145369;
  border-color: #86b0bd;
  background-color: transparent;
  -moz-appearance: auto;
  appearance: auto;
}

.topic-header-pagination
  :deep(.pagination-jump-input::-webkit-outer-spin-button),
.topic-header-pagination
  :deep(.pagination-jump-input::-webkit-inner-spin-button) {
  -webkit-appearance: inner-spin-button;
  opacity: 1;
  margin: 0;
}

.topic-header-pagination :deep(.pagination-total-pages) {
  font-size: 0.75rem;
  color: #145369 !important;
}

.topic-header-pagination :deep(.pagination-jump-button) {
  padding: 0.05rem 0.25rem;
  font-size: 0.75rem;
  line-height: 1.1;
  color: #145369;
  border-color: #86b0bd;
  background-color: #fff;
}

.topic-header-pagination :deep(.pagination-jump-button:hover) {
  color: #145369;
  border-color: #145369;
  background-color: #fff0dd;
}

.topic-sort-input-wrapper {
  position: relative;
  width: 100%;
}

.topic-sort-input {
  width: 100%;
  height: 28px;
  min-height: 28px;
  font-size: 0.82rem;
  font-weight: 700;
  border: 1px solid #145369;
  border-radius: 999px;
  background: #145369;
  color: #fff;
  padding: 0 1.45rem 0 0.7rem;
  line-height: 28px;
  cursor: pointer;
  text-overflow: ellipsis;
  white-space: nowrap;
  overflow: hidden;
  transition: padding-right 0.16s ease;
}

.topic-sort-input.topic-sort-input--expanded {
  padding-right: 0.7rem;
}

.topic-sort-input-dropdown {
  position: absolute;
  top: 50%;
  right: 0.55rem;
  transform: translateY(-50%);
  font-size: 0.82rem;
  color: #fff;
  pointer-events: auto;
  line-height: 1;
}

.topic-sort-menu {
  position: absolute;
  top: calc(100% + 0.3rem);
  left: 0;
  width: 100%;
  border-radius: 0.65rem;
  border: 1px solid #d1d3d4;
  background: #fff;
  box-shadow: 0 8px 18px rgba(20, 83, 105, 0.12);
  z-index: 30;
  overflow: hidden;
}

.topic-sort-item {
  display: block;
  width: 100%;
  border: none;
  background: transparent;
  color: #145369;
  text-align: left;
  padding: 0.45rem 0.7rem;
  font-size: 0.82rem;
  line-height: 1.2;
}

.topic-sort-item + .topic-sort-item {
  border-top: 1px solid rgba(209, 211, 212, 0.8);
}

.topic-sort-item.active,
.topic-sort-item:hover {
  background: #fff0dd;
}

.topic-table-header th:first-child {
  border-top-left-radius: 0.5rem;
}

.topic-table-header th:last-child {
  border-top-right-radius: 0.5rem;
}

.table-hover tbody tr:hover {
  background-color: rgba(255, 240, 221, 0.5) !important;
}

.forum-row {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.table {
  margin-bottom: 0;
}

.table-responsive {
  overflow-x: hidden;
  overflow-y: visible;
}

.table thead {
  font-weight: 600;
  font-size: 0.9rem;
}

.topic-list-table {
  width: 100%;
  table-layout: fixed;
}

.topic-col {
  width: 71.5%;
}

.created-col,
.last-reply-col {
  width: 12%;
}

.reply-col {
  width: 5.5%;
  min-width: 4.6rem;
  white-space: nowrap;
}

.topic-list-table th.created-col,
.topic-list-table th.last-reply-col,
.topic-list-table th.reply-col,
.topic-list-table td.created-col,
.topic-list-table td.last-reply-col,
.topic-list-table td.reply-col {
  padding: 0 !important;
  text-align: center;
  vertical-align: middle;
}

.topic-list--container-max-1200 .topic-col {
  width: 66.5%;
}

.topic-list--container-max-1200 .created-col,
.topic-list--container-max-1200 .last-reply-col {
  width: 13.25%;
}

.topic-list--container-max-1200 .reply-col {
  width: 7%;
  min-width: 4.5rem;
}

.topic-list--container-max-1200 .topic-list-table th.created-col,
.topic-list--container-max-1200 .topic-list-table th.last-reply-col,
.topic-list--container-max-1200 .topic-list-table th.reply-col,
.topic-list--container-max-1200 .topic-list-table td.created-col,
.topic-list--container-max-1200 .topic-list-table td.last-reply-col,
.topic-list--container-max-1200 .topic-list-table td.reply-col,
.topic-list--container-max-1200 .meta-date-text {
  font-size: 0.84rem;
  line-height: 1.2;
}

.topic-list--container-max-992 .topic-col {
  width: 65.5%;
}

.topic-list--container-max-992 .created-col,
.topic-list--container-max-992 .last-reply-col {
  width: 13.5%;
}

.topic-list--container-max-992 .reply-col {
  width: 7.5%;
  min-width: 4.5rem;
}

.topic-list--container-max-992 .topic-header-main-row {
  gap: 0.45rem !important;
}

.topic-list--container-max-992 .topic-sort-dropdown {
  width: min(var(--sort-control-width, 100%), 8.6rem);
}

.topic-list--container-max-992
  .topic-header-pagination
  :deep(.topic-header-pagination-list .page-link) {
  padding: 0.12rem 0.3rem;
  font-size: 0.7rem;
  min-width: 1.45rem;
}

.topic-list--container-max-992
  .topic-header-pagination
  :deep(.pagination-jump-shell) {
  margin-left: 0.25rem;
  padding: 0.05rem 0.22rem;
  font-size: 0.68rem;
}

.topic-list--container-max-992
  .topic-header-pagination
  :deep(.pagination-jump-wrap) {
  gap: 0.2rem !important;
}

.topic-list--container-max-992
  .topic-header-pagination
  :deep(.pagination-jump-input) {
  font-size: 0.68rem;
  min-width: calc(1ch + 1.1em);
}

.topic-list--container-max-992
  .topic-header-pagination
  :deep(.pagination-total-pages) {
  font-size: 0.68rem;
}

.topic-list--container-max-992
  .topic-header-pagination
  :deep(.pagination-jump-button) {
  padding: 0.04rem 0.2rem;
  font-size: 0.68rem;
}

.topic-list--container-max-992 .topic-list-table th.created-col,
.topic-list--container-max-992 .topic-list-table th.last-reply-col,
.topic-list--container-max-992 .topic-list-table th.reply-col,
.topic-list--container-max-992 .topic-list-table td.created-col,
.topic-list--container-max-992 .topic-list-table td.last-reply-col,
.topic-list--container-max-992 .topic-list-table td.reply-col,
.topic-list--container-max-992 .meta-date-text {
  font-size: 0.8rem;
  line-height: 1.15;
}

.topic-list--container-max-992 .reply-count-badge {
  min-width: 2rem;
  text-align: center;
}

.topic-list--container-max-992 .created-col :deep(.user-avatar-img),
.topic-list--container-max-992 .last-reply-col :deep(.user-avatar-img) {
  width: 22px !important;
  height: 22px !important;
}

.topic-list--container-max-768 .interaction-aligned-badge,
.topic-list--container-max-768 .reply-count-badge {
  min-height: 40px;
  height: 40px;
  padding: 0.35rem 0.65rem;
  font-size: 0.82rem;
}

.topic-list--container-max-768 .topic-title-link {
  font-size: 0.95rem;
}

.meta-cell-stack {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-start;
  width: 100%;
  height: 100%;
}

.meta-content-group {
  display: inline-flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.3rem;
  margin-top: auto;
}

.meta-user-row {
  display: inline-flex;
  align-items: center;
  text-align: left;
}

.meta-cell-center {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
}

.meta-date-text {
  text-align: left;
  white-space: normal;
  word-break: break-word;
  overflow-wrap: anywhere;
  line-height: 1.25;
}

/* 徽章樣式 */
.badge.bg-secondary {
  background-color: #86b0bd !important;
  color: #fff;
}

.badge.bg-secondary-light {
  background-color: #d0e8f2 !important;
  color: #145369;
}

.interaction-aligned-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 28px;
  min-height: 28px;
  padding: 0.2rem 0.45rem;
  margin-bottom: 0;
  font-size: 0.75rem;
  line-height: 1;
  white-space: nowrap;
  transition: all 0.3s ease;
}

.reply-count-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  height: 28px;
  min-height: 28px;
  padding: 0.2rem 0.45rem;
  margin-bottom: 0;
  font-size: 0.75rem;
  line-height: 1;
  white-space: nowrap;
  transition: all 0.3s ease;
}

.parent-link-btn {
  border: 1px solid #86b0bd;
  color: #145369;
  background-color: #d0e8f2;
  cursor: pointer;
}

.parent-link-btn:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

.badge.bg-primary {
  background-color: #86b0bd !important;
  color: #fff;
}

/* 主題標題 */
.topic-title-link {
  color: #0f3c4d;
  text-decoration: none;
  font-weight: 700;
  letter-spacing: 0.2px;
  font-size: 1.05rem;
  transition: color 0.2s ease;
}

.topic-title-link:hover {
  color: #86b0bd;
}

/* 互動按鈕容器樣式 */
.interaction-container {
  display: inline-flex;
  align-items: center;
  position: relative;
}

.reply-mode-tabs .nav-link.active {
  background: #145369;
  color: #fff;
}

.topic-mobile-card {
  border: 1px solid #d1d3d4;
  border-radius: 0.6rem;
  cursor: pointer;
}

.topic-mobile-card:active {
  transform: scale(0.998);
}

/* 載入動畫 */
.spinner-border {
  color: #145369 !important;
}

/* 響應式調整 */
@media (max-width: 1200px) and (min-width: 768px) {
.topic-col {
    width: 66.5%;
  }

.created-col,
  .last-reply-col {
    width: 13.25%;
  }

.reply-col {
    width: 7%;
    min-width: 4.5rem;
  }

.topic-list-table th.created-col,
  .topic-list-table th.last-reply-col,
  .topic-list-table th.reply-col,
  .topic-list-table td.created-col,
  .topic-list-table td.last-reply-col,
  .topic-list-table td.reply-col,
  .meta-date-text {
    font-size: 0.84rem;
    line-height: 1.2;
  }
}

@media (max-width: 992px) and (min-width: 768px) {
.topic-col {
    width: 65.5%;
  }

.created-col,
  .last-reply-col {
    width: 13.5%;
  }

.reply-col {
    width: 7.5%;
    min-width: 4.5rem;
  }

.topic-header-main-row {
    gap: 0.45rem !important;
  }

.topic-sort-dropdown {
    width: min(var(--sort-control-width, 100%), 8.6rem);
  }

.topic-header-pagination :deep(.topic-header-pagination-list .page-link) {
    padding: 0.12rem 0.3rem;
    font-size: 0.7rem;
    min-width: 1.45rem;
  }

.topic-header-pagination :deep(.pagination-jump-shell) {
    margin-left: 0.25rem;
    padding: 0.05rem 0.22rem;
    font-size: 0.68rem;
  }

.topic-header-pagination :deep(.pagination-jump-wrap) {
    gap: 0.2rem !important;
  }

.topic-header-pagination :deep(.pagination-jump-input) {
    font-size: 0.68rem;
    min-width: calc(1ch + 1.1em);
  }

.topic-header-pagination :deep(.pagination-total-pages) {
    font-size: 0.68rem;
  }

.topic-header-pagination :deep(.pagination-jump-button) {
    padding: 0.04rem 0.2rem;
    font-size: 0.68rem;
  }

.topic-list-table th.created-col,
  .topic-list-table th.last-reply-col,
  .topic-list-table th.reply-col,
  .topic-list-table td.created-col,
  .topic-list-table td.last-reply-col,
  .topic-list-table td.reply-col,
  .meta-date-text {
    font-size: 0.8rem;
    line-height: 1.15;
  }

.reply-count-badge {
    min-width: 2rem;
    text-align: center;
  }

.created-col :deep(.user-avatar-img),
  .last-reply-col :deep(.user-avatar-img) {
    width: 22px !important;
    height: 22px !important;
  }
}

@media (max-width: 768px) {
.interaction-aligned-badge {
    min-height: 40px;
    height: 40px;
    padding: 0.35rem 0.65rem;
    font-size: 0.82rem;
  }

.reply-count-badge {
    min-height: 40px;
    height: 40px;
    padding: 0.35rem 0.65rem;
    font-size: 0.82rem;
  }

.table {
    font-size: 0.9rem;
  }

.table td {
    padding: 0.75rem 0.5rem;
  }

.topic-title-link {
    font-size: 0.95rem;
  }
}

@media (max-width: 576px) {
.table {
    font-size: 0.85rem;
  }

.table td {
    padding: 0.6rem 0.5rem;
  }

.table thead {
    font-size: 0.8rem;
  }
}
</style>
