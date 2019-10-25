/*
  0 - ~17volt voltmeter
  works with 3.3volt and 5volt Arduinos
  uses the stable internal 1.1volt reference
  10k resistor from A0 to ground, and 150k resistor from A0 to +batt
  (1k8:27k or 2k2:33k are also valid 1:15 ratios)
  100n capacitor from A0 to ground for stable readings
*/


#include <SoftwareSerial.h>

int ignitionCoilPin = 2; // ignition coil power (using MOSFET)
// variables
unsigned long lastInterruptTime;

SoftwareSerial BTserial(6, 7); // RX | TX
// Connect the HC-08 TX to Arduino pin 2 RX.
// Connect the HC-08 RX to Arduino pin 3 TX
/*
  uses the stable internal 1.1volt reference
  10k resistor from A0 to ground, and 150k resistor from A0 to +batt
  (1k8:27k or 2k2:33k are also valid 1:15 ratios)
  100n capacitor from A0 to ground for stable readings
*/
long tachCountTime = 0;
volatile  long tachPulseCount = 0;
unsigned int VoltageCount; // holds readings
unsigned int VoltageTotal; // holds readings
float voltage; // converted to volt
long rpm = 0;
void setup() {
  pinMode(8, OUTPUT);

  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(ignitionCoilPin, INPUT);
  pinMode(digitalPinToInterrupt(ignitionCoilPin), INPUT_PULLUP);

  attachInterrupt(digitalPinToInterrupt(ignitionCoilPin), sparkPulse, FALLING);

  analogReference(DEFAULT); // use the internal ~1.1volt reference | change (INTERNAL) to (INTERNAL1V1) for a Mega
  Serial.begin(9600); // ---set serial monitor to this value---

  BTserial.begin(9600);
  delay(200);
  //Serial.write("AT");
}

void loop()
{
  digitalWrite(8, !digitalRead(8));

  // read battery volts every second and store result
  if (millis() % 1000 == 0) {
    ReadVoltage();
  }

  // calculate rpm every 25 pulses
  /*
      25 pulse per second
      = 1500 per minute
      4 ign pulses (one per cylinder) in 2 rotations (4 strokes) so 2 pulses per revolution
      1500/2 = 750 RPM
  */
  if (tachPulseCount >= 25)
  {
    // Disable INT;
    noInterrupts();
    float delta = millis() - tachCountTime;
    //Serial.println(delta);
    // Convert pulses to tachPulseCount
    float pulsePerMiliSecond = (float)tachPulseCount / delta;
    float pulsePerSecond = pulsePerSecond * 1000;
    rpm = ((pulsePerSecond * 60) / 2``);
    //Serial.print("TachCount:");
    // Serial.println(rpm);
    tachPulseCount = 0;
    tachCountTime = millis();
    // Enable INT
    interrupts();
  }
  if((millis() - tachCountTime)>4000) {
    // engine off / no sparks!
    rpm=0;
  }
  // output an update every 1/4 second
  if (millis() % 250 == 0) {
    char out[30];
    char str_rpm[6];
    /* 4 is mininum width, 2 is precision; float value is copied onto str_temp*/
    dtostrf(rpm, 4, 2, str_rpm);
    char str_volt[6];
    dtostrf(voltage, 4, 2, str_volt);
    snprintf(out, 30, "r%s:v%s",  str_rpm, str_volt);
    BTserial.write(out);
  }
}

// calculate rpm
void sparkPulse()
{
  digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));

  tachPulseCount++;

}


void ReadVoltage() {
  analogRead(A0); // one unused reading to clear any ghost charge
  int sensorValue = analogRead(A0);
  float volts = ( sensorValue * (5.0 / 1023.0)) * 3;

  VoltageTotal = VoltageTotal + sensorValue; // add each value
  VoltageCount++;



  float total = VoltageTotal / VoltageCount;
  voltage = (total * (5.0 / 1023.0)) * 3;
  /*
      Serial.print("The battery is ");
      Serial.print(voltage); // change to (voltage, 3) for three decimal places
      Serial.println(" volt");*/
  if (VoltageCount > 5) {
    VoltageTotal = 0;
    VoltageCount = 0;

  }
}
