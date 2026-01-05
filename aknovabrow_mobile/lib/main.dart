import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'screens/browser_screen.dart';

void main() {
  WidgetFlutterBinding.ensureInitialized();

  SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
]);

SystemChrome.setSystemUIOverlayStyle(
    const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
      systemNavigationBarColor: Color(0xFF263238),
      systemNavigationBarIconBrightness: Brightness.light,
    ),
  );
  
  runApp(const AknovaBrowser());
}

class AknovaBrowser extends StatelessWidget {
  const AknovaBrowser({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AKNOVA Browser',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        primaryColor: const Color(0xFF1976D2),
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF1976D2),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
        appBarTheme: const AppBarTheme(
          backgroundColor: Color(0xFF1976D2),
          foregroundColor: Colors.white,
          elevation: 0,
        ),
      ),
      home: const BrowserScreen(),
    );
  }
}