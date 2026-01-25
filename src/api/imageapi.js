import apiClient from './request'

/**
 * 上傳單張圖片
 * @param {File} file - 圖片檔案物件
 * @returns {Promise<string>} 返回圖片 URL
 */
export const uploadImage = async (file) => {
  const formData = new FormData()
  formData.append('file', file)

  try {
    // 使用 axios 上傳，需要覆蓋 Content-Type
    const response = await apiClient.post('/image', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
    
    // 假設後端返回 { url: "/img/xxx.jpg" } 或直接返回 url 字串
    return typeof response === 'string' ? response : response.url
  } catch (error) {
    console.error('圖片上傳失敗:', error)
    throw error
  }
}

/**
 * 上傳多張圖片
 * @param {FileList|Array} files - 圖片檔案列表
 * @returns {Promise<Array<string>>} 返回圖片 URL 陣列
 */
export const uploadMultipleImages = async (files) => {
  const uploadPromises = Array.from(files).map(file => uploadImage(file))
  
  try {
    const results = await Promise.all(uploadPromises)
    return results
  } catch (error) {
    console.error('批量上傳失敗:', error)
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
  uploadMultipleImages,
  deleteImage,
  deleteMultipleImages
}
