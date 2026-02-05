package oily.top.game;


import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import oily.top.network.Protocol;

public class World {
    private static final World instance = new World();
    
    private final Map<Integer, Player> players = new ConcurrentHashMap<>();
    private final Random random = new Random();
    
    private World() {}
    
    public static World getInstance() {
        return instance;
    }
    
    public synchronized Player createPlayer(String name) {
        Player player = new Player(name);
        
        // 随机出生位置
        player.setX(random.nextInt(700) + 50.0f);
        player.setY(random.nextInt(500) + 50.0f);
        
        players.put(player.getId(), player);
        return player;
    }
    
    public Player getPlayer(int playerId) {
        return players.get(playerId);
    }
    
    public List<Player> getAllPlayers() {
        return new ArrayList<>(players.values());
    }
    
    public List<Protocol.PlayerData> getAllPlayersData() {
        List<Protocol.PlayerData> result = new ArrayList<>();
        for (Player player : players.values()) {
            result.add(new Protocol.PlayerData(
                player.getId(),
                player.getName(),
                player.getX(),
                player.getY(),
                player.getDirection()
            ));
        }
        return result;
    }
    
    public Player removePlayer(int playerId) {
        return players.remove(playerId);
    }
    
    public int getPlayerCount() {
        return players.size();
    }
}