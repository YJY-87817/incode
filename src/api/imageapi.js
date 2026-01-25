import apiClient from '@/api/request'

/**
 * 上傳圖片（支持單張或多張）
 * @param {File|FileList|Array} files - 圖片檔案物件、檔案列表或陣列
 * @returns {Promise<string|Array<string>>} 返回圖片 URL 或 URL 陣列
 */
export const uploadImage = async (files) => {
  const formData = new FormData()
  const isSingleFile = files instanceof File

  // 處理單個 File 或多個檔案
  if (isSingleFile) {
    formData.append('files', files)
  } else {
    Array.from(files).forEach(file => {
      formData.append('files', file)
    })
  }

  try {
    const response = await apiClient.post('/images/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
    
    // 單檔案返回字符串，多檔案返回陣列
    return isSingleFile 
      ? (Array.isArray(response) ? response[0] : response)
      : (Array.isArray(response) ? response : [response])
  } catch (error) {
    console.error('圖片上傳失敗:', error)
    throw error
  }
}

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
  deleteImage,
  deleteMultipleImages
}
