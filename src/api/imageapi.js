import apiClient from '@/api/request'

// 取得不含 /api 的基底網址，方便把後端回傳的相對路徑轉為完整網址
const apiBaseUrl = (apiClient.defaults?.baseURL || '').replace(/\/api$/, '')

const toAbsoluteUrl = (url) => {
  if (!url) return ''
  if (/^https?:\/\//i.test(url)) return url
  if (url.startsWith('/')) return `${apiBaseUrl}${url}`
  return `${apiBaseUrl}/${url}`
}

const normalizeResponseUrls = (response) => {
  if (!response) return []

  // 後端回傳格式可能是：
  // 1. { urls: ['/img/abc.jpg', '/img/xyz.jpg'] }
  // 2. ['/img/abc.jpg', '/img/xyz.jpg']
  // 3. { data: { urls: [...] } }
  // 4. { data: [...] }
  let urls = []
  
  if (Array.isArray(response)) {
    urls = response
  } else if (response.urls && Array.isArray(response.urls)) {
    urls = response.urls
  } else if (response.data && Array.isArray(response.data)) {
    urls = response.data
  } else if (response.data && response.data.urls && Array.isArray(response.data.urls)) {
    urls = response.data.urls
  }

  return urls
    .map((u) => (typeof u === 'string' ? u.trim() : ''))
    .filter(Boolean)
    .map(toAbsoluteUrl)
}

/**
 * 上傳圖片（支持單張或多張）
 * @param {File|FileList|Array} files - 圖片檔案物件、檔案列表或陣列
 * @returns {Promise<string|Array<string>>} 返回圖片 URL 或 URL 陣列（已轉為絕對路徑）
 */
export const uploadImage = async (files) => {
  const formData = new FormData()
  const isSingleFile = files instanceof File

  // 處理單個 File 或多個檔案
  if (isSingleFile) {
    formData.append('files', files)
  } else {
    Array.from(files).forEach((file) => {
      formData.append('files', file)
    })
  }

  try {
    const response = await apiClient.post('/images/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })

    console.log('圖片上傳回應:', response)
    const normalizedUrls = normalizeResponseUrls(response)
    console.log('正規化後的 URLs:', normalizedUrls)
    
    if (normalizedUrls.length === 0) {
      throw new Error('後端未回傳圖片網址，回應:', JSON.stringify(response))
    }

    return isSingleFile ? normalizedUrls[0] : normalizedUrls
  } catch (error) {
    console.error('圖片上傳失敗:', error)
    console.error('錯誤詳情:', {
      message: error.message,
      response: error.response?.data,
      status: error.response?.status,
    })
    throw error
  }
}

/**
 * 多檔案上傳語意化別名（與 uploadImage 相同行為）
 */
export const uploadMultipleImages = async (files) => uploadImage(files)

/**
 * 刪除圖片
 * @param {string} fileNameOrUrl - 圖片檔名或 URL
 * @returns {Promise<void>}
 */
export const deleteImage = async (fileNameOrUrl) => {
  if (!fileNameOrUrl) {
    throw new Error('檔名無效')
  }

  try {
    // 如果是完整 URL，只取檔名部分
    const fileName = typeof fileNameOrUrl === 'string' 
      ? fileNameOrUrl.split('/').pop() 
      : fileNameOrUrl

    await apiClient.delete('/image', {
      params: { fileNameOrUrl: fileName }
    })
  } catch (error) {
    console.error('圖片刪除失敗:', error)
    throw error
  }
}

/**
 * 批量刪除圖片
 * @param {Array<string>} fileNames - 圖片檔名或 URL 陣列
 * @returns {Promise<void>}
 */
export const deleteMultipleImages = async (fileNames) => {
  if (!Array.isArray(fileNames) || fileNames.length === 0) {
    throw new Error('檔案列表無效')
  }

  try {
    const deletePromises = fileNames.map(fileName => deleteImage(fileName))
    await Promise.all(deletePromises)
  } catch (error) {
    console.error('批量刪除失敗:', error)
    throw error
  }
}

export default {
  uploadImage,
  uploadMultipleImages,
  deleteImage,
  deleteMultipleImages
}
