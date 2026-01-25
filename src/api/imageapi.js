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

export default {
  uploadImage,
  uploadMultipleImages
}
