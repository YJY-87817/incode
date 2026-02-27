<script setup>
import { computed, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import { useForumPost } from "@/composables/useForumPost";

const forumStore = useForumStore();
const router = useRouter();
const route = useRoute();
const { hasDraftContent } = useForumPost();

const allDrafts = computed(() => forumStore.drafts || []);

const getDraftExtraValues = (draft) => {
  if (!draft?.showEntrustMission) return [];
  const data = draft?.entrustMission || {};
  return [
    data.title,
    data.detail,
    data.rewardPoints,
    data.category,
    data.deadline,
  ];
};

const isCommentLikeDraft = (draft) => {
  const type = draft?.contentType;
  return type === CONTENT_TYPES.COMMENT || type === CONTENT_TYPES.COMMENT_REPLY;
};

const hasVisibleDraftContent = (draft) => {
  const titleToCheck = isCommentLikeDraft(draft) ? "" : draft.title;
  return hasDraftContent(titleToCheck, draft.body, getDraftExtraValues(draft));
};

const expandDraft = (draft) => {
  allDrafts.value.forEach((d) => {
    if (d.id !== draft.id && !d.isMinimized) {
      forumStore.toggleDraftMinimize(d.id, true);
    }
  });

  forumStore.toggleDraftMinimize(draft.id, false);
};

const closeDraft = (draft) => {
  forumStore.removeDraft(draft.id);
};

const scrollToReplyTarget = async (draft) => {
  const targetId = draft.replyingToContentId;
  let threadId = draft.replyingToThreadId;

  if (!targetId) return;

  if (!threadId) {
    threadId = await resolveThreadIdByContentId(targetId);
    if (threadId) {
      forumStore.updateDraft(draft.id, { replyingToThreadId: threadId });
    }
  }

  if (!threadId) return;

  forumStore.pendingScrollTargetId = targetId;

  const currentThreadId = route.params?.id ?? null;
  if (String(currentThreadId ?? "") !== String(threadId)) {
    router.push({ name: "forum-thread", params: { id: threadId } });
    return;
  }

  nextTick(() => {
    const targetEl = document.getElementById(`content-${targetId}`);
    if (targetEl) {
      const offsetTop =
        targetEl.getBoundingClientRect().top + window.scrollY - 60;
      window.scrollTo({ top: offsetTop, behavior: "smooth" });
      forumStore.pendingScrollTargetId = null;
    }
  });
};

const fetchContentDetail = async (contentId) => {
  const res = await apiClient.get(`/ContentApi/${contentId}`);
  return res?.contentId ? res : (res?.data ?? res);
};

const resolveThreadIdByContentId = async (contentId) => {
  try {
    const detail = await fetchContentDetail(contentId);
    if (!detail) return null;

    const type = detail.type ?? detail.contentType ?? detail.content_type;
    const currentId = detail.contentId ?? detail.id ?? detail.content_id;
    const parentId =
      detail.parentContentId ?? detail.parent_content_id ?? detail.parentId;

    if (type === CONTENT_TYPES.POST || !parentId) return currentId;
    if (type === CONTENT_TYPES.REPLY) return parentId;

    // COMMENT or COMMENT_REPLY: need to walk up
    const parent = await fetchContentDetail(parentId);
    if (!parent) return parentId;

    const parentType = parent.type ?? parent.contentType ?? parent.content_type;
    const parentParentId =
      parent.parentContentId ?? parent.parent_content_id ?? parent.parentId;

    if (parentType === CONTENT_TYPES.REPLY) {
      return parentParentId ?? parentId;
    }

    if (parentType === CONTENT_TYPES.POST) {
      return parentId;
    }

    if (parentType === CONTENT_TYPES.COMMENT) {
      if (!parentParentId) return parentId;
      const grandParent = await fetchContentDetail(parentParentId);
      if (!grandParent) return parentParentId;

      const gpType =
        grandParent.type ?? grandParent.contentType ?? grandParent.content_type;
      const gpParentId =
        grandParent.parentContentId ??
        grandParent.parent_content_id ??
        grandParent.parentId;

      if (gpType === CONTENT_TYPES.REPLY) {
        return gpParentId ?? parentParentId;
      }

      if (gpType === CONTENT_TYPES.POST) {
        return parentParentId;
      }
    }

    return parentId;
  } catch (error) {
    console.error("[NewTopicBanners] resolveThreadIdByContentId 失敗", error);
    return null;
  }
};
</script>

<template>
  <div class="forum-banners">
    <div
      v-if="allDrafts.some((d) => d.isMinimized && hasVisibleDraftContent(d))"
      class="banners-container"
    >
      <template v-for="draft in allDrafts" :key="`banner-${draft.id}`">
        <div
          v-if="draft.isMinimized && hasVisibleDraftContent(draft)"
          class="new-topic-banner"
          @click="expandDraft(draft)"
        >
          <div class="banner-content">
            <div
              v-if="draft.replyingToContentId || draft.parentContentId"
              class="banner-jump"
              @click.stop="scrollToReplyTarget(draft)"
            >
              <i class="fa-solid fa-people-pulling"></i>
            </div>
            <div class="banner-left">
              <div class="banner-title">
                <span v-if="draft.title.trim()">{{ draft.title }}</span>
                <span v-else class="text-muted">未命名的草稿</span>
              </div>
            </div>
            <div class="banner-right">
              <button
                type="button"
                class="btn btn-sm banner-close-btn"
                @click.stop="closeDraft(draft)"
              >
                <i class="fa-solid fa-xmark"></i>
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.forum-banners {
  width: 100%;
}

.banners-container {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.new-topic-banner {
  position: relative;
  background: transparent;
  border: 1px solid #d2d6da;
  border-radius: 0.375rem;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  animation: slideUp 0.3s ease-out;
}

@keyframes slideUp {
  from {
    transform: translateY(100%);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.banner-content {
  display: flex;
  align-items: stretch;
  justify-content: space-between;
  gap: 0;
  max-width: 100%;
}

.banner-left {
  flex: 1;
  min-width: 0;
  display: flex;
}

.banner-jump {
  flex: 0 0 20%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem 0.5rem;
  border-top-left-radius: 0.375rem;
  border-bottom-left-radius: 0.375rem;
  border-right: 1px solid #d2d6da;
  font-weight: 600;
  color: #145369;
  transition:
    background-color 0.2s ease,
    color 0.2s ease;
  user-select: none;
  cursor: pointer;
}

.banner-right {
  flex: 0 0 20%;
  display: flex;
  align-items: stretch;
}

.banner-title {
  flex: 1;
  min-width: 0;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  padding: 0.25rem;
  display: flex;
  align-items: center;
}

.banner-title .text-muted {
  color: #6c757d;
  font-style: italic;
}

.new-topic-banner {
  cursor: pointer;
  transition:
    background-color 0.2s ease,
    border-color 0.2s ease;
}

.new-topic-banner:hover {
  background-color: rgba(20, 83, 105, 0.08);
  border-color: #145369;
}

.new-topic-banner .banner-jump:hover {
  background-color: #145369;
  color: #fff;
}

.new-topic-banner .btn-sm {
  padding: 0.375rem 0.75rem;
  font-size: 0.875rem;
  white-space: nowrap;
}

.new-topic-banner .banner-close-btn {
  width: 100%;
  height: 100%;
  flex-shrink: 0;
  padding: 0 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  border-radius: 0 0.375rem 0.375rem 0;
  border-left: 1px solid #d2d6da;
  transition:
    background-color 0.2s ease,
    border-color 0.2s ease,
    color 0.2s ease;
}

.new-topic-banner .banner-close-btn:hover {
  background-color: #dc3545;
  border-color: #dc3545;
  color: #fff;
}
</style>
