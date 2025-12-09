import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
    plugins: [vue()],
    server: {
        port: 5173,
        host: '0.0.0.0'
    },
    // 设置基础路径为空字符串，确保资源路径正确
    base: '',
    // 生产环境构建配置
    build: {
        // 使用默认的esbuild压缩
        minify: 'esbuild',
        // 确保资源路径正确
        assetsDir: 'assets',
        rollupOptions: {
            output: {
                // 确保资源文件名一致性
                entryFileNames: 'assets/[name].[hash].js',
                chunkFileNames: 'assets/[name].[hash].js',
                assetFileNames: 'assets/[name].[hash].[ext]',
                manualChunks: {
                    // 将第三方库单独打包
                    vendor: ['vue', 'element-plus', '@element-plus/icons-vue'],
                    utils: ['axios', '@microsoft/signalr']
                }
            }
        }
    }
})