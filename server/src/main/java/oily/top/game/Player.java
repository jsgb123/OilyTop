package oily.top.game;

public class Player {
    private static int nextId = 1;
    
    private final int id;
    private String name;
    private float x;
    private float y;
    private float direction;
    private int level;
    private long experience;
    
    public Player(String name) {
        this.id = nextId++;
        this.name = name;
        this.x = 400.0f;
        this.y = 300.0f;
        this.direction = 0.0f;
        this.level = 1;
        this.experience = 0;
    }
    
    // Getters and Setters
    public int getId() { return id; }
    
    public String getName() { return name; }
    public void setName(String name) { this.name = name; }
    
    public float getX() { return x; }
    public void setX(float x) { this.x = x; }
    
    public float getY() { return y; }
    public void setY(float y) { this.y = y; }
    
    public float getDirection() { return direction; }
    public void setDirection(float direction) { this.direction = direction; }
    
    public int getLevel() { return level; }
    public void setLevel(int level) { this.level = level; }
    
    public long getExperience() { return experience; }
    public void setExperience(long experience) { this.experience = experience; }
    
    @Override
    public String toString() {
        return String.format("Player{id=%d, name='%s', pos=(%.1f, %.1f)}", 
            id, name, x, y);
    }
}