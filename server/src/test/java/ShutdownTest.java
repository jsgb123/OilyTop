public class ShutdownTest {
    public static void main(String[] args) throws Exception {
        // 创建日志文件
        java.io.File logFile = new java.io.File("shutdown_test.log");
        java.io.PrintWriter logWriter = new java.io.PrintWriter(
            new java.io.FileWriter(logFile, true), true);
        
        Runtime.getRuntime().addShutdownHook(new Thread(() -> {
            String message = new java.util.Date() + " [SHUTDOWN] 关闭钩子执行";
            System.out.println(message);  // 控制台输出
            logWriter.println(message);   // 文件输出
            logWriter.close();
            
            // 尝试让用户看到输出
            try {
                Thread.sleep(3000);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }));
        
        System.out.println("程序启动，PID: "  );
        System.out.println("测试方法：");
        System.out.println("1. 按 Ctrl+C - 会看到输出");
        System.out.println("2. 点击关闭按钮 - 输出可能看不到");
        System.out.println("3. 查看日志文件: " + logFile.getAbsolutePath());
        
        // 模拟运行
        while (true) {
            Thread.sleep(1000);
            System.out.println("运行中... " + new java.util.Date());
        }
    }
}