import axios from "axios";

const movies = ["Inception", "Interstellar", "You"];
const apiKey = "4c291a150e094e99974590cf8b03c4c6";

export default async function downloadPosters() {
  const results = [];

  try {
    for (const title of movies) {
      // 查詢電影
      const searchUrl = `https://api.themoviedb.org/3/search/movie?api_key=${apiKey}&query=${encodeURIComponent(title)}`;
      const searchRes = await axios.get(searchUrl, {
        withCredentials: false // Disable credentials to avoid CORS issues
      });
      const movie = searchRes.data.results[0];

      if (movie && movie.poster_path) {
        const posterUrl = `https://image.tmdb.org/t/p/w500${movie.poster_path}`;

        // Download the poster image
        const posterResponse = await axios.get(posterUrl, {
          responseType: 'arraybuffer',
          withCredentials: false
        });

        // Convert to base64 for display
        const base64 = btoa(
          new Uint8Array(posterResponse.data).reduce(
            (data, byte) => data + String.fromCharCode(byte),
            ''
          )
        );

        results.push({
          title: title,
          posterUrl: posterUrl,
          posterData: `data:${posterResponse.headers['content-type']};base64,${base64}`,
          movieId: movie.id,
          movieTitle: movie.title
        });

        console.log(`下載完成: ${title} - ${movie.title}`);
      }
    }

    return results;
  } catch (error) {
    console.error("Error downloading posters:", error);
    throw error;
  }
}
