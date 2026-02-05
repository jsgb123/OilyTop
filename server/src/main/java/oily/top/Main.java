package oily.top;

import oily.top.network.GameServer;
import oily.top.db.Database;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class Main {
    private static final Logger logger = LoggerFactory.getLogger(Main.class);
    
    public static void main(String[] args) {
        logger.info("=== OilyTop MMORPG Server 启动 ===");
        
        try {
            // 初始化数据库
            Database.getInstance().init();
            logger.info("数据库初始化完成");
            
            // 启动游戏服务器
            GameServer server = new GameServer(8080);
            server.start();
            
            logger.info("服务器已在端口 8080 启动");
            logger.info("按 Ctrl+C 停止服务器");
            
            // 保持服务器运行
            server.awaitTermination();
            
        } catch (Exception e) {
            logger.error("服务器启动失败", e);
            System.exit(1);
        }
    }
}