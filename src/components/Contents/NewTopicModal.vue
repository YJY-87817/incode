<script setup>
import { reactive, onMounted, watch, ref, computed } from "vue";
import { CONTENT_TYPES, useForumStore } from "@/stores/forumStore";
import apiClient from "@/api/request.js";
import TiptapEditor from "@/components/Contents/TiptapEditor.vue";
import { useForumPost } from "@/composables/useForumPost";
import Swal from "sweetalert2";

const forumStore = useForumStore();
const { getModeTitle, validatePost, publishPost, hasDraftContent } =
  useForumPost();

const modalMode = computed(() => forumStore.newTopic?.mode || "post");
const isPostMode = computed(() => modalMode.value === "post");
const isEditMode = computed(() => modalMode.value === "edit");
const isEditingCommentLike = computed(() => {
  if (!isEditMode.value) return false;
  const type = forumStore.newTopic?.contentType;
  return type === CONTENT_TYPES.COMMENT || type === CONTENT_TYPES.COMMENT_REPLY;
});
const isReplyLikeMode = computed(
  () => modalMode.value === "comment" || modalMode.value === "commentReply",
);
const shouldHideTitleInput = computed(
  () => isEditingCommentLike.value || isReplyLikeMode.value,
);
const modalTitle = computed(() => getModeTitle(modalMode.value));
const submitLabel = computed(() => {
  if (modalMode.value === "edit") return "更新";
  return modalMode.value === "post" ? "發佈" : "發佈";
});

const buildReplyTitle = (baseTitle) => {
  const base = String(baseTitle || "").trim();
  return base ? `Re: ${base}` : "Re:";
};

const replyAutoTitle = computed(() => {
  const data = forumStore.replyingToContentData;
  const baseTitle = data?.title || data?.contentText || "";
  return buildReplyTitle(baseTitle);
});

const newTopicState = reactive({
  title: forumStore.newTopic.title || "",
  body: forumStore.newTopic.body || "",
  categoryId: forumStore.newTopic.categoryId ?? null,
  parentContentId: forumStore.newTopic.parentContentId ?? null,
  topics: [],
});

const categorySearch = ref(forumStore.newTopic.categorySearch || "");
const showCategoryDropdown = ref(false);
const subCategorySearch = ref("");
const showSubCategoryDropdown = ref(false);
const replySearch = ref("");
const showReplyDropdown = ref(false);
const isCreatingCategory = ref(false);
const isCreatingSubCategory = ref(false);
const showCreateCategoryModal = ref(false);
const newCategoryName = ref("");
const newCategoryDescription = ref("");
const createCategoryParentId = ref(null);
const showCategoryDescriptionModal = ref(false);
const showCategoryNameModal = ref(false);
const showTitleBodyRequiredModal = ref(false);

const showEntrustMission = ref(Boolean(forumStore.newTopic.showEntrustMission));
const entrustMissionState = reactive({
  title: forumStore.newTopic.entrustMission?.title || "",
  detail: forumStore.newTopic.entrustMission?.detail || "",
  rewardPoints: forumStore.newTopic.entrustMission?.rewardPoints || "",
  category: forumStore.newTopic.entrustMission?.category || "",
  deadline: forumStore.newTopic.entrustMission?.deadline || "",
});

const canUseEntrustMission = computed(
  () => isPostMode.value && !newTopicState.parentContentId,
);

const syncEntrustMissionFromStore = () => {
  showEntrustMission.value = Boolean(forumStore.newTopic.showEntrustMission);
  entrustMissionState.title = forumStore.newTopic.entrustMission?.title || "";
  entrustMissionState.detail = forumStore.newTopic.entrustMission?.detail || "";
  entrustMissionState.rewardPoints =
    forumStore.newTopic.entrustMission?.rewardPoints || "";
  entrustMissionState.category =
    forumStore.newTopic.entrustMission?.category || "";
  entrustMissionState.deadline =
    forumStore.newTopic.entrustMission?.deadline || "";
};

const NO_REPLY_LABEL = "無(發佈新主題)";

const getCategoryIdValue = (cat) =>
  cat?.id ??
  cat?.content_category_id ??
  cat?.categoryId ??
  cat?.category_id ??
  null;

const getParentCategoryIdValue = (cat) =>
  cat?.parentCategoryId ??
  cat?.parent_category_id ??
  cat?.parentCategoryID ??
  cat?.parent_id ??
  null;

const getCategoryName = (cat) =>
  cat?.name ?? cat?.categoryName ?? cat?.category_name ?? "";

const getTopicTitle = (topic) => topic?.title ?? topic?.name ?? "";

const toCategoryKey = (value) =>
  value === null || value === undefined ? null : String(value);

const rootCategories = computed(() => {
  const categories = forumStore.categories || [];
  return categories.filter((cat) => !getParentCategoryIdValue(cat));
});

const showSubCategoryColumn = computed(() => Boolean(newTopicState.categoryId));

const filteredCategories = computed(() => {
  const keyword = categorySearch.value.trim().toLowerCase();
  const categories = rootCategories.value;
  if (!keyword) return categories;
  return categories.filter((cat) =>
    String(getCategoryName(cat) || "")
      .toLowerCase()
      .includes(keyword),
  );
});

const categorySuggestions = computed(() => {
  const input = categorySearch.value.trim();
  if (!input) {
    return rootCategories.value.slice(0, 12);
  }
  return filteredCategories.value.slice(0, 12);
});

const selectedSubCategoryId = ref(null);

const childCategories = computed(() => {
  if (!newTopicState.categoryId) return [];
  const parentKey = toCategoryKey(newTopicState.categoryId);
  return (forumStore.categories || []).filter(
    (cat) => toCategoryKey(getParentCategoryIdValue(cat)) === parentKey,
  );
});

const filteredSubCategories = computed(() => {
  const keyword = subCategorySearch.value.trim().toLowerCase();
  const categories = childCategories.value;
  if (!keyword) return categories;
  return categories.filter((cat) =>
    String(getCategoryName(cat) || "")
      .toLowerCase()
      .includes(keyword),
  );
});

const subCategorySuggestions = computed(() => {
  const input = subCategorySearch.value.trim();
  if (!input) {
    return childCategories.value.slice(0, 12);
  }
  return filteredSubCategories.value.slice(0, 12);
});

const filteredReplyTopics = computed(() => {
  const keyword = replySearch.value.trim().toLowerCase();
  const topics = newTopicState.topics || [];

  // 過濾主題
  let filtered = topics;
  if (keyword) {
    filtered = filtered.filter((topic) =>
      String(getTopicTitle(topic) || "")
        .toLowerCase()
        .includes(keyword),
    );
  }

  // 按最新回文時間排序（有回文的優先，沒回文的按建立時間排序）
  return filtered.sort((a, b) => {
    const aTime = a.lastReplyTime
      ? new Date(a.lastReplyTime).getTime()
      : new Date(a.createdAt).getTime();
    const bTime = b.lastReplyTime
      ? new Date(b.lastReplyTime).getTime()
      : new Date(b.createdAt).getTime();
    return bTime - aTime; // 由新至舊
  });
});

const replySuggestions = computed(() => {
  const input = replySearch.value.trim();
  if (!input) {
    // 按最新回文時間排序
    const topics = (newTopicState.topics || []).slice();
    return topics
      .sort((a, b) => {
        const aTime = a.lastReplyTime
          ? new Date(a.lastReplyTime).getTime()
          : new Date(a.createdAt).getTime();
        const bTime = b.lastReplyTime
          ? new Date(b.lastReplyTime).getTime()
          : new Date(b.createdAt).getTime();
        return bTime - aTime; // 由新至舊
      })
      .slice(0, 12);
  }
  return filteredReplyTopics.value.slice(0, 12);
});

const effectiveCategoryId = computed(
  () => selectedSubCategoryId.value ?? newTopicState.categoryId ?? null,
);

const fetchTopics = (categoryId = null) => {
  const params = {};
  if (categoryId) params.categoryId = categoryId;
  // 後端現在直接提供回覆統計資訊
  params.pageSize = 9999;

  apiClient
    .get("/ContentApi", { params })
    .then((data) => {
      const allItems = data.items || [];

      // 後端現在直接在 ContentDto 中提供回覆統計資訊
      // 不再需要前端計算，直接使用後端提供的資料
      newTopicState.topics = allItems.map((item) => {
        return {
          id: item.contentId,
          title: item.title,
          // 使用後端提供的統計資訊
          replyCount: item.replyCount ?? 0,
          lastReplyTime: item.lastReplyTime ?? null,
          lastReplyUser: item.lastReplyUser ?? null,
          lastReplyUserId: item.lastReplyUserId ?? null,
          createdAt: item.createdAt,
        };
      });
    })
    .catch((error) => {
      console.error("載入主題失敗:", error);
    });
};

const syncCategorySelection = (categoryId) => {
  if (!categoryId) {
    newTopicState.categoryId = null;
    selectedSubCategoryId.value = null;
    return;
  }

  const categories = forumStore.categories || [];
  const targetKey = toCategoryKey(categoryId);
  const target = categories.find(
    (cat) => toCategoryKey(getCategoryIdValue(cat)) === targetKey,
  );

  if (!target) {
    newTopicState.categoryId = categoryId;
    selectedSubCategoryId.value = null;
    return;
  }

  const parentId = getParentCategoryIdValue(target);
  if (parentId) {
    newTopicState.categoryId = parentId;
    selectedSubCategoryId.value = getCategoryIdValue(target);
  } else {
    newTopicState.categoryId = getCategoryIdValue(target);
    selectedSubCategoryId.value = null;
  }

  const parentMatch = categories.find(
    (cat) =>
      toCategoryKey(getCategoryIdValue(cat)) ===
      toCategoryKey(newTopicState.categoryId),
  );
  if (parentMatch?.name) {
    categorySearch.value = getCategoryName(parentMatch);
  }
};

onMounted(() => {
  if (!forumStore.categories?.length) {
    forumStore.fetchCategories();
  }
  // 同步 store 中選中的分類
  if (forumStore.selectedCategory) {
    syncCategorySelection(forumStore.selectedCategory);
  }

  if (!categorySearch.value && newTopicState.categoryId) {
    const match = (forumStore.categories || []).find(
      (cat) =>
        toCategoryKey(getCategoryIdValue(cat)) ===
        toCategoryKey(newTopicState.categoryId),
    );
    if (match?.name) {
      categorySearch.value = getCategoryName(match);
    }
  }
});

watch(
  () => forumStore.showNewTopicModal,
  (value) => {
    if (value) {
      syncEntrustMissionFromStore();
    }
  },
);

watch(
  () => showEntrustMission.value,
  (value) => {
    forumStore.newTopic.showEntrustMission = value;
    if (!value) {
      resetEntrustMission();
      forumStore.newTopic.entrustMission = {
        title: "",
        detail: "",
        rewardPoints: "",
        category: "",
        deadline: "",
      };
    }
  },
);

watch(
  () => ({ ...entrustMissionState }),
  (value) => {
    forumStore.newTopic.entrustMission = {
      title: value.title || "",
      detail: value.detail || "",
      rewardPoints: value.rewardPoints || "",
      category: value.category || "",
      deadline: value.deadline || "",
    };
  },
  { deep: true },
);

watch(
  () => newTopicState.title,
  (value) => {
    forumStore.newTopic.title = value;
  },
);

watch(
  () => [isReplyLikeMode.value, replyAutoTitle.value],
  ([replyLikeMode, autoTitle]) => {
    if (!replyLikeMode) return;
    if (!autoTitle) return;
    if (newTopicState.title !== autoTitle) {
      newTopicState.title = autoTitle;
    }
  },
  { immediate: true },
);

watch(
  () => newTopicState.body,
  (value) => {
    forumStore.newTopic.body = value;
  },
);

watch(
  () => newTopicState.categoryId,
  (value) => {
    selectedSubCategoryId.value = null;
    subCategorySearch.value = "";
  },
);

watch(
  () => effectiveCategoryId.value,
  (value) => {
    forumStore.newTopic.categoryId = value;
  },
);

watch(
  () => newTopicState.parentContentId,
  (value) => {
    forumStore.newTopic.parentContentId = value;
    if (!value) {
      replySearch.value = "";
      return;
    }
    if (showEntrustMission.value) {
      showEntrustMission.value = false;
      resetEntrustMission();
    }
    if (modalMode.value !== "post") {
      return;
    }
    const match = (newTopicState.topics || []).find(
      (topic) => String(topic.id) === String(value),
    );
    if (match) {
      replySearch.value = getTopicTitle(match) || "";
    }
  },
);

watch(
  () => replySearch.value,
  (value) => {
    if (modalMode.value !== "post") return;
    const input = String(value || "").trim();
    if (!input || input === NO_REPLY_LABEL) {
      newTopicState.parentContentId = null;
      return;
    }

    const match = (newTopicState.topics || []).find(
      (topic) =>
        String(getTopicTitle(topic) || "").toLowerCase() ===
        input.toLowerCase(),
    );
    newTopicState.parentContentId = match ? match.id : null;
  },
);

watch(
  () => canUseEntrustMission.value,
  (value) => {
    if (value) return;
    if (showEntrustMission.value) {
      showEntrustMission.value = false;
      resetEntrustMission();
    }
  },
);

// 監聽 store 的 selectedCategory 變化
watch(
  () => forumStore.selectedCategory,
  (newCategory) => {
    if (newCategory) {
      syncCategorySelection(newCategory);
    }
  },
);

watch(
  () => forumStore.categories,
  (categories) => {
    if (!categories?.length) return;
    if (forumStore.newTopic.categoryId) {
      syncCategorySelection(forumStore.newTopic.categoryId);
    } else if (!categorySearch.value && newTopicState.categoryId) {
      const match = (categories || []).find(
        (cat) =>
          toCategoryKey(getCategoryIdValue(cat)) ===
          toCategoryKey(newTopicState.categoryId),
      );
      if (match?.name) {
        categorySearch.value = getCategoryName(match);
      }
    }
  },
);

watch(
  () => selectedSubCategoryId.value,
  (value) => {
    if (!value) {
      subCategorySearch.value = "";
      return;
    }
    const match = childCategories.value.find(
      (cat) => toCategoryKey(getCategoryIdValue(cat)) === toCategoryKey(value),
    );
    if (match) {
      subCategorySearch.value = getCategoryName(match) || "";
    }
  },
);

watch(
  () => subCategorySearch.value,
  (value) => {
    const input = String(value || "").trim();
    if (!input) {
      selectedSubCategoryId.value = null;
      return;
    }

    const match = childCategories.value.find(
      (cat) =>
        String(getCategoryName(cat) || "").toLowerCase() ===
        input.toLowerCase(),
    );
    selectedSubCategoryId.value = match ? getCategoryIdValue(match) : null;
  },
);

watch(
  () => effectiveCategoryId.value,
  (newCat) => {
    const shouldPreserveParent =
      forumStore.newTopic.mode !== "post" &&
      Boolean(forumStore.newTopic.parentContentId);

    if (!shouldPreserveParent) {
      newTopicState.parentContentId = null;
      replySearch.value = "";
    }
    if (newCat) {
      fetchTopics(newCat);
    } else {
      newTopicState.topics = [];
    }
  },
);

watch(
  () => newTopicState.topics,
  (topics) => {
    if (modalMode.value !== "post") return;
    if (!newTopicState.parentContentId) return;
    const match = (topics || []).find(
      (topic) => String(topic.id) === String(newTopicState.parentContentId),
    );
    if (!match) {
      newTopicState.parentContentId = null;
      replySearch.value = "";
      return;
    }
    if (!replySearch.value) {
      replySearch.value = getTopicTitle(match) || "";
    }
  },
);

watch(
  () => categorySearch.value,
  (value) => {
    forumStore.newTopic.categorySearch = value;
    const input = String(value || "").trim();
    if (!input) {
      newTopicState.categoryId = null;
      return;
    }

    const match = rootCategories.value.find(
      (cat) =>
        String(getCategoryName(cat) || "").toLowerCase() ===
        input.toLowerCase(),
    );
    newTopicState.categoryId = match ? getCategoryIdValue(match) : null;
  },
);

// 監聽 modal 打開時同步分類選擇
watch(
  () => forumStore.showNewTopicModal,
  (isOpen) => {
    if (isOpen) {
      newTopicState.title = forumStore.newTopic.title || "";
      newTopicState.body = forumStore.newTopic.body || "";

      // 編輯模式下，直接同步分類（不依賴於搜索）
      if (
        forumStore.newTopic.mode === "edit" &&
        forumStore.newTopic.categoryId
      ) {
        newTopicState.categoryId = forumStore.newTopic.categoryId;
        selectedSubCategoryId.value = null;
        // 同時更新搜索框以顯示當前分類
        const currentCategory = (forumStore.categories || []).find(
          (cat) =>
            toCategoryKey(getCategoryIdValue(cat)) ===
            toCategoryKey(forumStore.newTopic.categoryId),
        );
        if (currentCategory?.name) {
          categorySearch.value = getCategoryName(currentCategory);
        }
        return;
      }
    }

    if (isOpen && forumStore.newTopic.categoryId) {
      syncCategorySelection(forumStore.newTopic.categoryId);
      if (forumStore.newTopic.categorySearch && !categorySearch.value) {
        categorySearch.value = forumStore.newTopic.categorySearch;
      }

      if (forumStore.newTopic.parentContentId) {
        newTopicState.parentContentId = forumStore.newTopic.parentContentId;
      }

      // 如果有 parentContentId，需要確保主題列表已加載
      if (forumStore.newTopic.parentContentId) {
        const currentEffectiveId = effectiveCategoryId.value;
        if (
          currentEffectiveId &&
          (!newTopicState.topics || newTopicState.topics.length === 0)
        ) {
          // 主題列表還未加載，觸發加載
          fetchTopics(currentEffectiveId);
        }
      }
    }

    if (
      isOpen &&
      !forumStore.newTopic.parentContentId &&
      forumStore.newTopic.mode !== "post"
    ) {
      forumStore.newTopic.mode = "post";
      forumStore.newTopic.contentType = CONTENT_TYPES.POST;
    }
  },
  { immediate: false, flush: "post" },
);

// 監聽 newTopicState.topics 加載完成後，自動填充 replySearch
watch(
  () => newTopicState.topics,
  () => {
    if (forumStore.showNewTopicModal && forumStore.newTopic.parentContentId) {
      const parentId = forumStore.newTopic.parentContentId;
      const match = (newTopicState.topics || []).find(
        (topic) => String(topic.id) === String(parentId),
      );
      if (match && !replySearch.value) {
        replySearch.value = getTopicTitle(match) || "";
      }
    }
  },
);

const selectCategoryFromDropdown = (category) => {
  if (!category) return;
  categorySearch.value = getCategoryName(category) || "";
  newTopicState.categoryId = getCategoryIdValue(category);
  showCategoryDropdown.value = false;
};

const selectSubCategoryFromDropdown = (category) => {
  if (!category) return;
  subCategorySearch.value = getCategoryName(category) || "";
  selectedSubCategoryId.value = getCategoryIdValue(category);
  showSubCategoryDropdown.value = false;
};

const selectReplyFromDropdown = (topic) => {
  if (!topic) {
    replySearch.value = "";
    newTopicState.parentContentId = null;
    showReplyDropdown.value = false;
    return;
  }
  replySearch.value = getTopicTitle(topic) || "";
  newTopicState.parentContentId = topic.id;
  showReplyDropdown.value = false;
};

const hideCategoryDropdown = () => {
  setTimeout(() => {
    showCategoryDropdown.value = false;
  }, 150);
};

const hideSubCategoryDropdown = () => {
  setTimeout(() => {
    showSubCategoryDropdown.value = false;
  }, 150);
};

const hideReplyDropdown = () => {
  setTimeout(() => {
    showReplyDropdown.value = false;
  }, 150);
};

const createCategoryFromSearch = async () => {
  const name = categorySearch.value.trim();
  if (!name || isCreatingCategory.value) return;

  const existing = (forumStore.categories || []).find(
    (cat) =>
      String(getCategoryName(cat) || "").toLowerCase() === name.toLowerCase(),
  );
  if (existing) {
    newTopicState.categoryId = getCategoryIdValue(existing);
    return;
  }

  newCategoryName.value = name;
  newCategoryDescription.value = "";
  createCategoryParentId.value = null;
  showCreateCategoryModal.value = true;
};

const createSubCategoryFromSearch = async () => {
  const parentId = newTopicState.categoryId;
  const name = subCategorySearch.value.trim();
  if (!parentId || !name || isCreatingSubCategory.value) return;

  const existing = childCategories.value.find(
    (cat) =>
      String(getCategoryName(cat) || "").toLowerCase() === name.toLowerCase(),
  );
  if (existing) {
    selectedSubCategoryId.value = getCategoryIdValue(existing);
    return;
  }

  newCategoryName.value = name;
  newCategoryDescription.value = "";
  createCategoryParentId.value = parentId;
  showCreateCategoryModal.value = true;
};

const submitCreateCategory = async () => {
  const name = newCategoryName.value.trim();
  if (!name) {
    showCategoryNameModal.value = true;
    return;
  }

  const description = newCategoryDescription.value.trim();
  if (!description) {
    showCategoryDescriptionModal.value = true;
    return;
  }

  try {
    const parentId = createCategoryParentId.value ?? null;
    if (parentId) {
      isCreatingSubCategory.value = true;
    } else {
      isCreatingCategory.value = true;
    }

    const created = await forumStore.createCategory(
      name,
      description,
      parentId,
    );

    if (parentId) {
      if (created?.id) {
        selectedSubCategoryId.value = created.id;
        subCategorySearch.value = created.name || name;
      } else {
        const fallback = childCategories.value.find((cat) => cat.name === name);
        if (fallback) {
          selectedSubCategoryId.value = fallback.id;
          subCategorySearch.value = fallback.name || name;
        }
      }
    } else if (created?.id) {
      newTopicState.categoryId = created.id;
      categorySearch.value = created.name || name;
    } else {
      const fallback = (forumStore.categories || []).find(
        (cat) => cat.name === name,
      );
      if (fallback) {
        newTopicState.categoryId = fallback.id;
        categorySearch.value = fallback.name || name;
      }
    }

    showCreateCategoryModal.value = false;
  } catch (error) {
    console.error("新增分類失敗:", error);
    alert("新增分類失敗，請稍後再試");
  } finally {
    isCreatingCategory.value = false;
    isCreatingSubCategory.value = false;
  }
};

const closeCreateCategoryModal = () => {
  showCreateCategoryModal.value = false;
  createCategoryParentId.value = null;
};

const closeCategoryDescriptionModal = () => {
  showCategoryDescriptionModal.value = false;
};

const closeCategoryNameModal = () => {
  showCategoryNameModal.value = false;
};

const closeTitleBodyRequiredModal = () => {
  showTitleBodyRequiredModal.value = false;
};

const resetEntrustMission = () => {
  entrustMissionState.title = "";
  entrustMissionState.detail = "";
  entrustMissionState.rewardPoints = "";
  entrustMissionState.category = "";
  entrustMissionState.deadline = "";
};

const toggleEntrustMission = () => {
  if (!canUseEntrustMission.value) return;
  showEntrustMission.value = !showEntrustMission.value;
  if (!showEntrustMission.value) {
    resetEntrustMission();
  }
};

const validateEntrustMission = async () => {
  if (!showEntrustMission.value) return true;

  const title = String(entrustMissionState.title || "").trim();
  const detail = String(entrustMissionState.detail || "").trim();
  const category = String(entrustMissionState.category || "").trim();
  const rewardPoints = Number(entrustMissionState.rewardPoints);

  if (!title || !detail || !category || !Number.isFinite(rewardPoints)) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "請完整填寫委託任務資訊",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return false;
  }

  if (rewardPoints <= 0) {
    await Swal.fire({
      icon: "warning",
      title: "提醒",
      text: "獎勵點數需大於 0",
      confirmButtonText: "確定",
      confirmButtonColor: "#145369",
    });
    return false;
  }

  return true;
};

const buildEntrustMissionPayload = () => {
  if (!showEntrustMission.value) return null;

  const rewardPoints = Number(entrustMissionState.rewardPoints);
  const deadline = String(entrustMissionState.deadline || "").trim();

  return {
    title: String(entrustMissionState.title || "").trim(),
    detail: String(entrustMissionState.detail || "").trim(),
    rewardPoints: Number.isFinite(rewardPoints) ? rewardPoints : 0,
    category: String(entrustMissionState.category || "").trim(),
    deadline: deadline || null,
  };
};

const getModalExtraValues = () => {
  if (!showEntrustMission.value) return [];
  return [
    entrustMissionState.title,
    entrustMissionState.detail,
    entrustMissionState.rewardPoints,
    entrustMissionState.category,
    entrustMissionState.deadline,
  ];
};

const submitNewTopic = async () => {
  // 判斷是否為編輯模式
  const isEdit = modalMode.value === "edit";

  if (showEntrustMission.value && !(await validateEntrustMission())) {
    return;
  }

  const entrustMissionPayload = buildEntrustMissionPayload();

  // 使用 composable 的 publishPost 進行驗證和提交
  const success = await publishPost(
    {
      title: newTopicState.title,
      body: newTopicState.body,
      categoryId: effectiveCategoryId.value,
      parentContentId: newTopicState.parentContentId,
      contentType: forumStore.newTopic?.contentType,
      editingContentId: forumStore.newTopic?.editingContentId,
      entrustMission: entrustMissionPayload,
    },
    isEdit,
  );

  if (!success) {
    return;
  }

  // 如果是從草稿打開的，刪除該草稿
  if (forumStore.currentEditingDraftId) {
    forumStore.removeDraft(forumStore.currentEditingDraftId);
    forumStore.currentEditingDraftId = null;
  }

  // 關閉 modal 和重置內容
  resetNewTopicDraft();
  closeNewTopic();
};

const closeNewTopic = () => {
  forumStore.toggleNewTopicModal(false);
};

const resetNewTopicDraft = () => {
  newTopicState.title = "";
  newTopicState.body = "";
  newTopicState.categoryId = null;
  newTopicState.parentContentId = null;
  selectedSubCategoryId.value = null;
  categorySearch.value = "";
  subCategorySearch.value = "";
  replySearch.value = "";
  showEntrustMission.value = false;
  resetEntrustMission();
  forumStore.newTopic.showEntrustMission = false;
  forumStore.newTopic.entrustMission = {
    title: "",
    detail: "",
    rewardPoints: "",
    category: "",
    deadline: "",
  };
  forumStore.newTopic.title = "";
  forumStore.newTopic.body = "";
  forumStore.newTopic.categoryId = null;
  forumStore.newTopic.parentContentId = null;
  forumStore.newTopic.categorySearch = "";
  forumStore.newTopic.mode = "post";
  forumStore.newTopic.contentType = CONTENT_TYPES.POST;
  forumStore.newTopic.editingContentId = null;
};

const handleModalCancel = () => {
  const isEditModeActive = modalMode.value === "edit";
  const hasContent = hasDraftContent(
    newTopicState.title,
    newTopicState.body,
    getModalExtraValues(),
  );

  if (hasContent) {
    // 檢查是否是從草稿打開的
    if (forumStore.currentEditingDraftId) {
      // 同步內容回原草稿
      forumStore.updateDraft(forumStore.currentEditingDraftId, {
        title: newTopicState.title,
        body: newTopicState.body,
        categoryId: newTopicState.categoryId,
        parentContentId: newTopicState.parentContentId,
        categorySearch: categorySearch.value,
        showEntrustMission: showEntrustMission.value,
        entrustMission: buildEntrustMissionPayload() || {
          title: "",
          detail: "",
          rewardPoints: "",
          category: "",
          deadline: "",
        },
      });
      // 回到 sidebar 編輯器
      forumStore.toggleDraftMinimize(forumStore.currentEditingDraftId, false);
    } else {
      // 創建新草稿並最小化
      const draftId = forumStore.createDraft({
        title: newTopicState.title,
        body: newTopicState.body,
        categoryId: newTopicState.categoryId,
        parentContentId: newTopicState.parentContentId,
        categorySearch: categorySearch.value,
        mode: forumStore.newTopic.mode,
        contentType: forumStore.newTopic.contentType,
        editingContentId: forumStore.newTopic.editingContentId,
        replyingToContentId: forumStore.replyingToContentId,
        replyingToContentData: forumStore.replyingToContentData,
        replyingToThreadId: forumStore.replyingToThreadId,
        showEntrustMission: showEntrustMission.value,
        entrustMission: buildEntrustMissionPayload() || {
          title: "",
          detail: "",
          rewardPoints: "",
          category: "",
          deadline: "",
        },
      });
      forumStore.toggleDraftMinimize(draftId, !isEditModeActive);
    }

    forumStore.toggleNewTopicModal(false);
    forumStore.currentEditingDraftId = null;

    // 清空 modal 的內容
    resetNewTopicDraft();
    return;
  }

  // 沒有內容時，只關閉 modal，不建立草稿
  if (forumStore.currentEditingDraftId) {
    forumStore.removeDraft(forumStore.currentEditingDraftId);
  }
  forumStore.currentEditingDraftId = null;
  resetNewTopicDraft();
  closeNewTopic();
};
</script>

<template>
  <Teleport to="body">
    <div
      v-if="forumStore.showNewTopicModal"
      class="custom-modal-backdrop"
      @click.self="handleModalCancel"
    >
      <div class="modal-dialog">
        <div class="modal-content">
          <form @submit.prevent="submitNewTopic">
            <div
              class="modal-header d-flex justify-content-between align-items-center mb-3"
            >
              <h5 class="modal-title mb-0">{{ modalTitle }}</h5>
              <button
                type="button"
                class="btn-close"
                @click="handleModalCancel"
              ></button>
            </div>
            <div class="modal-body">
              <div v-if="isPostMode" class="row g-3">
                <div
                  :class="
                    showSubCategoryColumn
                      ? 'col-12 col-md-4'
                      : effectiveCategoryId && newTopicState.topics.length
                        ? 'col-12 col-md-6'
                        : 'col-12'
                  "
                >
                  <div class="mb-3">
                    <label class="form-label">分類</label>
                    <div class="category-input-wrapper">
                      <input
                        v-model="categorySearch"
                        class="form-control category-input"
                        list="category-options"
                        placeholder="輸入或搜尋分類..."
                        @focus="showCategoryDropdown = true"
                        @blur="hideCategoryDropdown"
                        @keydown.enter.prevent
                        @keyup.enter.prevent="
                          categorySuggestions.length === 0
                            ? createCategoryFromSearch()
                            : null
                        "
                      />
                      <button
                        v-if="categorySearch"
                        type="button"
                        class="category-input-clear"
                        aria-label="清除分類輸入"
                        @mousedown.prevent
                        @click="categorySearch = ''"
                      >
                        ×
                      </button>
                      <span
                        class="category-input-dropdown"
                        aria-hidden="true"
                        @mousedown.prevent="showCategoryDropdown = true"
                      >
                        ▾
                      </span>
                    </div>
                    <datalist id="category-options">
                      <option
                        v-for="cat in categorySuggestions"
                        :key="getCategoryIdValue(cat)"
                        :value="getCategoryName(cat)"
                      />
                    </datalist>

                    <div
                      v-if="showCategoryDropdown"
                      class="category-dropdown border rounded mt-2"
                    >
                      <button
                        v-for="cat in categorySuggestions"
                        :key="getCategoryIdValue(cat)"
                        type="button"
                        class="dropdown-item w-100 text-start"
                        @mousedown.prevent="selectCategoryFromDropdown(cat)"
                      >
                        {{ getCategoryName(cat) }}
                      </button>
                      <div
                        v-if="categorySuggestions.length === 0"
                        class="text-muted small px-3 py-2"
                      >
                        沒有符合的分類
                      </div>
                    </div>

                    <div
                      class="d-flex justify-content-between align-items-center mt-2"
                    >
                      <button
                        v-if="categorySearch && filteredCategories.length === 0"
                        type="button"
                        class="btn btn-sm btn-forum-secondary"
                        :disabled="isCreatingCategory"
                        @click="createCategoryFromSearch"
                      >
                        {{ isCreatingCategory ? "建立中..." : "手動新增分類" }}
                      </button>
                    </div>
                  </div>
                </div>
                <div v-if="showSubCategoryColumn" class="col-12 col-md-4">
                  <div class="mb-3">
                    <label class="form-label">子分類</label>
                    <div class="category-input-wrapper">
                      <input
                        v-model="subCategorySearch"
                        class="form-control category-input"
                        list="subcategory-options"
                        placeholder="輸入或搜尋子分類..."
                        @focus="showSubCategoryDropdown = true"
                        @blur="hideSubCategoryDropdown"
                        @keydown.enter.prevent
                      />
                      <button
                        v-if="subCategorySearch"
                        type="button"
                        class="category-input-clear"
                        aria-label="清除子分類輸入"
                        @mousedown.prevent
                        @click="subCategorySearch = ''"
                      >
                        ×
                      </button>
                      <span
                        class="category-input-dropdown"
                        aria-hidden="true"
                        @mousedown.prevent="showSubCategoryDropdown = true"
                      >
                        ▾
                      </span>
                    </div>
                    <datalist id="subcategory-options">
                      <option
                        v-for="child in subCategorySuggestions"
                        :key="getCategoryIdValue(child)"
                        :value="getCategoryName(child)"
                      />
                    </datalist>

                    <div
                      v-if="showSubCategoryDropdown"
                      class="category-dropdown border rounded mt-2"
                    >
                      <button
                        v-for="child in subCategorySuggestions"
                        :key="getCategoryIdValue(child)"
                        type="button"
                        class="dropdown-item w-100 text-start"
                        @mousedown.prevent="
                          selectSubCategoryFromDropdown(child)
                        "
                      >
                        {{ getCategoryName(child) }}
                      </button>
                      <div
                        v-if="subCategorySuggestions.length === 0"
                        class="text-muted small px-3 py-2"
                      >
                        沒有符合的子分類
                      </div>
                    </div>

                    <div
                      class="d-flex justify-content-between align-items-center mt-2"
                    >
                      <button
                        v-if="
                          childCategories.length === 0 ||
                          filteredSubCategories.length === 0
                        "
                        type="button"
                        class="btn btn-sm btn-forum-secondary"
                        :disabled="isCreatingSubCategory || !subCategorySearch"
                        @click="createSubCategoryFromSearch"
                      >
                        {{
                          isCreatingSubCategory ? "建立中..." : "手動新增分類"
                        }}
                      </button>
                    </div>
                  </div>
                </div>
                <div
                  v-if="effectiveCategoryId && newTopicState.topics.length"
                  :class="
                    showSubCategoryColumn
                      ? 'col-12 col-md-4'
                      : 'col-12 col-md-6'
                  "
                >
                  <div class="mb-3">
                    <label class="form-label">回覆至(可選)</label>
                    <div class="category-input-wrapper">
                      <input
                        v-model="replySearch"
                        class="form-control category-input"
                        list="reply-options"
                        placeholder="輸入或搜尋主題..."
                        @focus="showReplyDropdown = true"
                        @blur="hideReplyDropdown"
                        @keydown.enter.prevent
                      />
                      <button
                        v-if="replySearch"
                        type="button"
                        class="category-input-clear"
                        aria-label="清除回覆輸入"
                        @mousedown.prevent
                        @click="replySearch = ''"
                      >
                        ×
                      </button>
                      <span
                        class="category-input-dropdown"
                        aria-hidden="true"
                        @mousedown.prevent="showReplyDropdown = true"
                      >
                        ▾
                      </span>
                    </div>
                    <datalist id="reply-options">
                      <option
                        v-for="topic in replySuggestions"
                        :key="topic.id"
                        :value="getTopicTitle(topic)"
                      />
                    </datalist>

                    <div
                      v-if="showReplyDropdown"
                      class="category-dropdown border rounded mt-2"
                    >
                      <button
                        type="button"
                        class="dropdown-item w-100 text-start"
                        @mousedown.prevent="selectReplyFromDropdown(null)"
                      >
                        {{ NO_REPLY_LABEL }}
                      </button>
                      <button
                        v-for="topic in replySuggestions"
                        :key="topic.id"
                        type="button"
                        class="dropdown-item w-100 text-start reply-dropdown-item"
                        @mousedown.prevent="selectReplyFromDropdown(topic)"
                      >
                        <div
                          class="d-flex justify-content-between align-items-center w-100"
                        >
                          <span class="reply-topic-title">{{
                            getTopicTitle(topic)
                          }}</span>
                          <span class="reply-topic-badge">
                            <span class="badge bg-primary me-2">{{
                              topic.replyCount
                            }}</span>
                            <small class="text-muted">{{
                              topic.lastReplyTime
                                ? new Date(
                                    topic.lastReplyTime,
                                  ).toLocaleDateString("zh-TW")
                                : "無回文"
                            }}</small>
                          </span>
                        </div>
                      </button>
                      <div
                        v-if="replySuggestions.length === 0"
                        class="text-muted small px-3 py-2"
                      >
                        沒有符合的主題
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="mb-3"></div>
              <div v-if="!shouldHideTitleInput" class="mb-3">
                <label class="form-label">標題</label>
                <input
                  v-model="newTopicState.title"
                  class="form-control"
                  autofocus
                  @keydown.enter.prevent
                />
              </div>
              <div class="mb-3">
                <label class="form-label">內容</label>
                <TiptapEditor
                  v-model="newTopicState.body"
                  placeholder="請輸入貼文內容..."
                />
              </div>
              <div v-if="canUseEntrustMission" class="mb-3">
                <button
                  type="button"
                  class="btn btn-outline-primary btn-entrust-toggle"
                  @click="toggleEntrustMission"
                >
                  {{ showEntrustMission ? "取消委託任務" : "發佈委託任務" }}
                </button>
              </div>
              <div
                v-if="showEntrustMission"
                class="entrust-mission-panel border rounded p-3 mb-3"
              >
                <h6 class="mb-3">委託任務資訊</h6>
                <div class="row g-3">
                  <div class="col-12 col-md-6">
                    <label class="form-label">任務標題</label>
                    <input
                      v-model="entrustMissionState.title"
                      class="form-control"
                      placeholder="請輸入任務標題"
                    />
                  </div>
                  <div class="col-12 col-md-6">
                    <label class="form-label">任務分類</label>
                    <input
                      v-model="entrustMissionState.category"
                      class="form-control"
                      placeholder="請輸入任務分類"
                    />
                  </div>
                  <div class="col-12 col-md-6">
                    <label class="form-label">獎勵點數</label>
                    <input
                      v-model="entrustMissionState.rewardPoints"
                      type="number"
                      min="1"
                      step="1"
                      class="form-control"
                      placeholder="請輸入獎勵點數"
                    />
                  </div>
                  <div class="col-12 col-md-6">
                    <label class="form-label">截止日期</label>
                    <input
                      v-model="entrustMissionState.deadline"
                      type="date"
                      class="form-control"
                    />
                  </div>
                  <div class="col-12">
                    <label class="form-label">任務說明</label>
                    <textarea
                      v-model="entrustMissionState.detail"
                      class="form-control"
                      rows="4"
                      placeholder="請輸入任務內容"
                    ></textarea>
                  </div>
                </div>
              </div>
            </div>
            <div class="modal-footer d-flex justify-content-end">
              <button
                class="btn btn-forum-secondary me-2"
                @click="handleModalCancel"
              >
                取消
              </button>
              <button type="submit" class="btn btn-forum-primary">
                {{ submitLabel }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div
      v-if="showCreateCategoryModal"
      class="create-category-backdrop"
      @click.self="closeCreateCategoryModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <form @submit.prevent="submitCreateCategory">
            <div
              class="modal-header d-flex justify-content-between align-items-center mb-3"
            >
              <h5 class="modal-title mb-0">
                {{ createCategoryParentId ? "新增子分類" : "新增分類" }}
              </h5>
              <button
                type="button"
                class="btn-close"
                @click="closeCreateCategoryModal"
              ></button>
            </div>
            <div class="modal-body">
              <div class="mb-3">
                <label class="form-label">分類名稱</label>
                <input v-model="newCategoryName" class="form-control" />
              </div>
              <div class="mb-3">
                <label class="form-label">描述</label>
                <textarea
                  v-model="newCategoryDescription"
                  class="form-control"
                  rows="3"
                  placeholder="請輸入分類描述"
                ></textarea>
              </div>
            </div>
            <div class="modal-footer d-flex justify-content-end">
              <button
                type="button"
                class="btn btn-forum-secondary me-2"
                @click="closeCreateCategoryModal"
              >
                取消
              </button>
              <button type="submit" class="btn btn-forum-primary">送出</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div
      v-if="showCategoryDescriptionModal"
      class="create-category-backdrop"
      @click.self="closeCategoryDescriptionModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <div
            class="modal-header d-flex justify-content-between align-items-center mb-3"
          >
            <h5 class="modal-title mb-0">提醒</h5>
            <button
              type="button"
              class="btn-close"
              @click="closeCategoryDescriptionModal"
            ></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">分類描述為必填，請先填寫描述。</p>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button
              type="button"
              class="btn btn-forum-primary"
              @click="closeCategoryDescriptionModal"
            >
              我知道了
            </button>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="showCategoryNameModal"
      class="create-category-backdrop"
      @click.self="closeCategoryNameModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <div
            class="modal-header d-flex justify-content-between align-items-center mb-3"
          >
            <h5 class="modal-title mb-0">提醒</h5>
            <button
              type="button"
              class="btn-close"
              @click="closeCategoryNameModal"
            ></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">分類名稱為必填，請先填寫名稱。</p>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button
              type="button"
              class="btn btn-forum-primary"
              @click="closeCategoryNameModal"
            >
              我知道了
            </button>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="showTitleBodyRequiredModal"
      class="create-category-backdrop"
      @click.self="closeTitleBodyRequiredModal"
    >
      <div class="create-category-dialog">
        <div class="create-category-content">
          <div
            class="modal-header d-flex justify-content-between align-items-center mb-3"
          >
            <h5 class="modal-title mb-0">提醒</h5>
            <button
              type="button"
              class="btn-close"
              @click="closeTitleBodyRequiredModal"
            ></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">請填寫標題與內容</p>
          </div>
          <div class="modal-footer d-flex justify-content-end">
            <button
              type="button"
              class="btn btn-forum-primary"
              @click="closeTitleBodyRequiredModal"
            >
              我知道了
            </button>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
/* 自定義按鈕樣式 */
.btn-forum-primary {
  background-color: #145369;
  border-color: #145369;
  color: #fff0dd;
  transition: all 0.3s ease;
}

.btn-forum-primary:hover {
  background-color: #86b0bd;
  border-color: #86b0bd;
  color: #fff;
}

.btn-forum-secondary {
  background-color: transparent;
  border: 1px solid #d1d3d4;
  color: #145369;
  transition: all 0.3s ease;
}

.btn-forum-secondary:hover {
  background-color: #fff0dd;
  border-color: #86b0bd;
  color: #145369;
}

.btn-entrust-toggle {
  border-color: #145369;
  color: #145369;
}

.btn-entrust-toggle:hover {
  background-color: #eaf2f4;
  color: #0f4456;
}

.btn-entrust-toggle:focus,
.btn-entrust-toggle:active,
.btn-entrust-toggle.active {
  background-color: #eaf2f4;
  border-color: #145369;
  color: #0f4456;
  box-shadow: none;
}

.entrust-mission-panel {
  background: #f6f8f9;
  border-color: #d9e2e6;
}

.custom-modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(67, 47, 40, 0.5);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.modal-dialog {
  background: #fff;
  border-radius: 0.3rem;
  max-width: 90%;
  width: 100%;
  padding: 1rem;
  position: relative;
  z-index: 2100;
  border: 1px solid #d1d3d4;
  max-height: 90vh;
  overflow-y: auto;
}

.create-category-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(67, 47, 40, 0.5);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.create-category-dialog {
  background: #fff;
  border-radius: 0.3rem;
  width: min(520px, 92%);
  padding: 0.75rem 1rem;
  position: relative;
  z-index: 2100;
  border: 1px solid #d1d3d4;
  max-height: 75vh;
  overflow-y: auto;
}

.create-category-content .form-control,
.create-category-content textarea.form-control {
  font-size: 0.95rem;
}

.create-category-content textarea.form-control {
  min-height: 100px;
}

.modal-header {
  border-bottom-color: #d1d3d4;
}

.modal-footer {
  border-top-color: #d1d3d4;
}

.form-control,
.form-select {
  border: 1px solid #d1d3d4;
  padding-left: 0.75rem;
}

.category-input-wrapper {
  position: relative;
}

.category-input {
  padding-right: 3.5rem;
}

.category-input-clear {
  position: absolute;
  top: 50%;
  right: 2rem;
  transform: translateY(-50%);
  border: none;
  background: transparent;
  color: #9aa0a6;
  font-size: 1.1rem;
  line-height: 1;
  padding: 0.25rem;
  cursor: pointer;
}

.category-input-clear:hover {
  color: #145369;
}

.category-input-dropdown {
  position: absolute;
  top: 50%;
  right: 0.75rem;
  transform: translateY(-50%);
  color: #9aa0a6;
  font-size: 0.9rem;
  pointer-events: auto;
}

.form-control:focus,
.form-select:focus {
  border-color: #86b0bd;
  box-shadow: 0 0 0 0.2rem rgba(134, 176, 189, 0.25);
}

.category-dropdown {
  max-height: 220px;
  overflow-y: auto;
  background: #fff;
  box-shadow: 0 8px 16px rgba(20, 83, 105, 0.12);
  border-color: #d1d3d4 !important;
}

.dropdown-item {
  padding: 0.5rem 0.75rem;
  border: none;
  background: transparent;
  color: #145369;
}

.dropdown-item:hover {
  background: rgba(255, 240, 221, 0.7);
}

.reply-dropdown-item {
  padding: 0.25rem 0.75rem !important;
}

.reply-topic-title {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 60%;
}

.reply-topic-badge {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-shrink: 0;
  margin-left: 0.5rem;
  white-space: nowrap;
}

.reply-topic-badge .badge {
  padding: 0.35rem 0.6rem;
  font-size: 0.75rem;
}
</style>
