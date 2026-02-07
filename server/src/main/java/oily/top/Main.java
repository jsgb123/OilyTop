package oily.top;

import oily.top.network.GameServer;
import oily.top.db.Database;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class Main {

    private static final Logger logger = LoggerFactory.getLogger(Main.class);
    private static GameServer gameServer;

    public static void main(String[] args) {
        logger.info("=== OilyTop MMORPG Server 启动 ===");

        try {
            // 关闭钩子,Ctrl+C 关闭时执行
            Runtime.getRuntime().addShutdownHook(new Thread(() -> {
                logger.info("正在关闭资源...");
                Database.getInstance().close();
                gameServer.stop();
                logger.info("数据库和服务器已关闭");
            }));
            // 初始化数据库
            Database.getInstance().init();
            logger.info("数据库初始化完成");

            gameServer = new GameServer(8080);
            // 启动服务器线程
            Thread serverThread = new Thread(() -> {
                try {
                    gameServer.start();
                } catch (InterruptedException e) {
                    logger.info("服务器线程被中断");
                } catch (Exception e) {
                    logger.error("服务器运行异常", e);
                }
            });

            // 保持服务器运行
            serverThread.start();
            logger.info("服务器已在端口 8080 启动");
            logger.info("按 Ctrl+C 停止服务器");

        } catch (Exception e) {
            logger.error("服务器启动失败", e);
            System.exit(1);
        }
    }
}
