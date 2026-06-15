// ============================================================================
//  إعدادات Firebase  —  Firebase configuration
// ----------------------------------------------------------------------------
//  املأ القيم التالية من:
//  Firebase Console > Project settings (⚙️) > General > Your apps > SDK setup
//
//  ملاحظة أمان: هذه المفاتيح "عامة" بطبيعتها في Firebase للويب، وليست سرّاً.
//  الحماية الحقيقية تأتي من:
//    1) قواعد Firestore (راجع ملف firestore.rules)
//    2) النطاقات المصرّح بها (Authentication > Settings > Authorized domains)
// ============================================================================

export const firebaseConfig = {
  apiKey:            "REPLACE_ME",
  authDomain:        "REPLACE_ME.firebaseapp.com",
  projectId:         "REPLACE_ME",
  storageBucket:     "REPLACE_ME.appspot.com",
  messagingSenderId: "REPLACE_ME",
  appId:             "REPLACE_ME"
};
