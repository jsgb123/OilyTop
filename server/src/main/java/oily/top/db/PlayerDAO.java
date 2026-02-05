package oily.top.db;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.sql.*;
import java.util.ArrayList;
import java.util.List;
import oily.top.game.Player;

public class PlayerDAO {
    private static final Logger logger = LoggerFactory.getLogger(PlayerDAO.class);
    
    public void savePlayer(Player player) throws SQLException {
        String sql = "            INSERT INTO players (account_id, name, level, experience, \n" +
"                                 position_x, position_y, last_login)\n" +
"            VALUES (?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)\n" +
"            ON DUPLICATE KEY UPDATE\n" +
"                level = VALUES(level),\n" +
"                experience = VALUES(experience),\n" +
"                position_x = VALUES(position_x),\n" +
"                position_y = VALUES(position_y),\n" +
"                last_login = VALUES(last_login)";
        
        try (Connection conn = Database.getInstance().getConnection();
             PreparedStatement pstmt = conn.prepareStatement(sql)) {
            
            pstmt.setInt(1, 1); // 临时account_id
            pstmt.setString(2, player.getName());
            pstmt.setInt(3, player.getLevel());
            pstmt.setLong(4, player.getExperience());
            pstmt.setFloat(5, player.getX());
            pstmt.setFloat(6, player.getY());
            
            pstmt.executeUpdate();
            logger.debug("玩家数据已保存: {}", player.getName());
        }
    }
    
    public List<Player> loadAllPlayers() throws SQLException {
        List<Player> players = new ArrayList<>();
        String sql = "SELECT id, name, level, experience, position_x, position_y FROM players";
        
        try (Connection conn = Database.getInstance().getConnection();
             Statement stmt = conn.createStatement();
             ResultSet rs = stmt.executeQuery(sql)) {
            
            while (rs.next()) {
                Player player = new Player(rs.getString("name"));
                // 注意：这里需要设置ID，但Player的ID是自动生成的
                // 在实际项目中需要更复杂的处理
                player.setLevel(rs.getInt("level"));
                player.setExperience(rs.getLong("experience"));
                player.setX(rs.getFloat("position_x"));
                player.setY(rs.getFloat("position_y"));
                
                players.add(player);
            }
        }
        
        return players;
    }
}