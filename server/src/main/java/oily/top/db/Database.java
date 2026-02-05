package oily.top.db;

import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.sql.Connection;
import java.sql.SQLException;
import java.sql.Statement;

public class Database {
    private static final Logger logger = LoggerFactory.getLogger(Database.class);
    private static final Database instance = new Database();
    
    private HikariDataSource dataSource;
    
    private Database() {}
    
    public static Database getInstance() {
        return instance;
    }
    
    public void init() {
        try {
            // H2数据库配置
            HikariConfig config = new HikariConfig();
            config.setJdbcUrl("jdbc:h2:file:./data/oilytop_db;DB_CLOSE_DELAY=-1");
            config.setUsername("sa");
            config.setPassword("");
            config.setMaximumPoolSize(10);
            config.setMinimumIdle(2);
            config.addDataSourceProperty("cachePrepStmts", "true");
            config.addDataSourceProperty("prepStmtCacheSize", "250");
            config.addDataSourceProperty("prepStmtCacheSqlLimit", "2048");
            
            dataSource = new HikariDataSource(config);
            
            // 创建表
            createTables();
            
            logger.info("数据库连接池初始化成功");
            
        } catch (Exception e) {
            logger.error("数据库初始化失败", e);
            throw new RuntimeException("数据库初始化失败", e);
        }
    }
    
    private void createTables() throws SQLException {
        try (Connection conn = dataSource.getConnection();
             Statement stmt = conn.createStatement()) {
            
            // 玩家表
            stmt.execute("CREATE TABLE IF NOT EXISTS players ( " +
"                    id INT AUTO_INCREMENT PRIMARY KEY, " +
"                    account_id INT NOT NULL, " +
"                    name VARCHAR(50) UNIQUE NOT NULL, " +
"                    level INT DEFAULT 1, " +
"                    experience BIGINT DEFAULT 0, " +
"                    class_config CLOB, " +
"                    position_x FLOAT DEFAULT 400.0, " +
"                    position_y FLOAT DEFAULT 300.0, " +
"                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP, " +
"                    last_login TIMESTAMP " +
"                )");
            
            // 物品表
            stmt.execute("CREATE TABLE IF NOT EXISTS player_items ( " +
"                    id INT AUTO_INCREMENT PRIMARY KEY, " +
"                    player_id INT NOT NULL, " +
"                    item_id INT NOT NULL, " +
"                    count INT DEFAULT 1, " +
"                    position INT, " +
"                    attributes CLOB, " +
"                    FOREIGN KEY (player_id) REFERENCES players(id) " +
"                )");
            
            logger.info("数据库表创建完成");
        }
    }
    
    public Connection getConnection() throws SQLException {
        return dataSource.getConnection();
    }
    
    public void close() {
        if (dataSource != null && !dataSource.isClosed()) {
            dataSource.close();
            logger.info("数据库连接池已关闭");
        }
    }
}