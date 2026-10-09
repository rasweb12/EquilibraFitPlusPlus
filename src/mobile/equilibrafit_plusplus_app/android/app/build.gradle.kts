import java.util.Properties

plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("dev.flutter.flutter-gradle-plugin")
}

val keystoreProperties = Properties()
val keystorePropertiesFile = rootProject.file("key.properties")
if (keystorePropertiesFile.exists()) {
    keystorePropertiesFile.inputStream().use { keystoreProperties.load(it) }
}

fun releaseSigningValue(propertyName: String, environmentName: String): String? {
    return (keystoreProperties[propertyName] as String?) ?: System.getenv(environmentName)
}

val releaseStoreFile = releaseSigningValue("storeFile", "ANDROID_KEYSTORE_PATH")
val releaseStorePassword = releaseSigningValue("storePassword", "ANDROID_KEYSTORE_PASSWORD")
val releaseKeyAlias = releaseSigningValue("keyAlias", "ANDROID_KEY_ALIAS")
val releaseKeyPassword = releaseSigningValue("keyPassword", "ANDROID_KEY_PASSWORD")
val hasReleaseSigning = !releaseStoreFile.isNullOrBlank()
    && !releaseStorePassword.isNullOrBlank()
    && !releaseKeyAlias.isNullOrBlank()
    && !releaseKeyPassword.isNullOrBlank()

val originalApplicationId = "br.com.equilibrafit.app"
val plusplusApplicationId = providers.gradleProperty("PLUSPLUS_APPLICATION_ID").orNull
    ?: System.getenv("EQUILIBRAFIT_PLUSPLUS_APPLICATION_ID")
val releaseRequested = gradle.startParameter.taskNames.any { it.contains("Release", ignoreCase = true) }
if (releaseRequested && (plusplusApplicationId.isNullOrBlank() || plusplusApplicationId == originalApplicationId)) {
    throw GradleException("Configure a distinct PLUSPLUS_APPLICATION_ID for the new Google Play application.")
}
if (releaseRequested && !hasReleaseSigning) {
    throw GradleException("Configure the new application's release signing credentials outside Git.")
}

android {
    namespace = "br.com.equilibrafit.app"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        applicationId = plusplusApplicationId?.takeIf { it.isNotBlank() && it != originalApplicationId }
            ?: "$originalApplicationId.plusplus.dev"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        create("release") {
            if (hasReleaseSigning) {
                storeFile = file(releaseStoreFile!!)
                storePassword = releaseStorePassword
                keyAlias = releaseKeyAlias
                keyPassword = releaseKeyPassword
            }
        }
    }

    buildTypes {
        debug {
            if (!plusplusApplicationId.isNullOrBlank() && plusplusApplicationId != originalApplicationId) {
                applicationIdSuffix = ".dev"
            }
        }
        release {
            signingConfig = signingConfigs.getByName("release")
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}
